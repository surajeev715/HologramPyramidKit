//Browser side of the remote link (see RemoteLink.cs), used by WebRemoteLinkPlatform.
//Connects browsers directly to each other with WebRTC (PeerJS). One browser is the host and shows a QR code,
//the others (remotes) open the same page with ?join=<hostId>.
//
//This file only transports: it opens connections, sends strings and queues what arrives.
//What the messages mean is decided in C#. Unity collects the queued events once per frame with
//WebRemoteLink_PollEvent, nothing here ever calls into Unity.
//
//An event is one string "kind\npeerId\ndata". Kinds: loaded, open, connected, data, closed, signaling, error
//(keep in sync with PeerEventKind in PeerEvent.cs and TryParseKind in WebRemoteLinkPlatform.cs).
var WebRemoteLink = {
    $WebRemoteLinkState: {
        //Data events are dropped above this, so a remote that floods messages can not fill the memory.
        MAX_QUEUED_EVENTS: 256,
        RECONNECT_INTERVAL_MS: 2000,

        peer: null,
        connections: {},   // peerId -> open or opening connection
        events: [],
        maxMessageChars: 0,
        reconnectTimer: null,
        wakeLockWanted: false,

        pushEvent: function (kind, peerId, data) {
            var state = WebRemoteLinkState;
            if (kind === "data" && state.events.length >= state.MAX_QUEUED_EVENTS) {
                return;
            }
            state.events.push(kind + "\n" + (peerId || "") + "\n" + (data || ""));
        },

        stop: function () {
            var state = WebRemoteLinkState;
            var oldPeer = state.peer;
            //Cleared first: destroy() fires the close handlers, they must not report the old connections.
            state.peer = null;
            state.connections = {};
            state.events.length = 0;
            clearInterval(state.reconnectTimer);
            state.reconnectTimer = null;
            if (oldPeer) {
                oldPeer.destroy();
            }
        },

        createPeer: function (id, maxMessageChars) {
            var state = WebRemoteLinkState;
            state.stop();
            state.maxMessageChars = maxMessageChars;

            //STUN only. PeerJS's defaults also include PeerJS's public TURN relay, which would silently
            //send our data through their server when a direct connection fails. We want to see that failure.
            var options = {
                debug: 1,
                config: { iceServers: [{ urls: "stun:stun.l.google.com:19302" }] }
            };
            var peer = id ? new window.Peer(id, options) : new window.Peer(options);
            state.peer = peer;

            peer.on("error", function (error) {
                if (state.peer !== peer) return;
                state.pushEvent("error", String(error.type), String(error.message || ""));
            });
            return peer;
        },

        watchConnection: function (connection) {
            var state = WebRemoteLinkState;
            var id = connection.peer;

            if (state.connections[id]) {
                //A second connection from the same peer is refused.
                connection.on("open", function () { connection.close(); });
                return;
            }
            state.connections[id] = connection;

            var isCurrent = function () { return state.connections[id] === connection; };

            connection.on("open", function () {
                if (isCurrent()) state.pushEvent("connected", id, "");
            });
            connection.on("data", function (data) {
                if (!isCurrent()) return;
                //Never trust what arrives over the network. Anything that is not a short string is passed on
                //as an empty message, which C# counts as invalid.
                var isValid = typeof data === "string" && data.length <= state.maxMessageChars;
                state.pushEvent("data", id, isValid ? data : "");
            });
            connection.on("close", function () {
                if (!isCurrent()) return;
                delete state.connections[id];
                state.pushEvent("closed", id, "");
            });
            connection.on("error", function (error) {
                if (!isCurrent()) return;
                delete state.connections[id];
                state.pushEvent("closed", id, String((error && error.message) || ""));
                connection.close();
            });
        }
    },

    //Loads one library (PeerJS, QR code) from a URL. Reports "loaded", or "error" with peerId "library".
    WebRemoteLink_LoadScript: function (urlPtr) {
        var state = WebRemoteLinkState;
        var url = UTF8ToString(urlPtr);
        var script = document.createElement("script");
        script.src = url;
        script.onload = function () { state.pushEvent("loaded", "", url); };
        script.onerror = function () { state.pushEvent("error", "library", url); };
        document.head.appendChild(script);
    },

    //1 once both libraries are loaded and the browser supports WebRTC, 0 otherwise.
    WebRemoteLink_IsReady: function () {
        return (window.Peer && window.qrcode && window.RTCPeerConnection) ? 1 : 0;
    },

    //Fills the buffer with cryptographically secure random bytes.
    WebRemoteLink_FillRandom: function (bufferPtr, length) {
        var bytes = new Uint8Array(length);
        crypto.getRandomValues(bytes);
        HEAPU8.set(bytes, bufferPtr);
    },

    //Registers this browser as a host under the given ID. Reports "open" with the ID once remotes can join,
    //and again after the signaling server was lost and came back.
    WebRemoteLink_StartHost: function (hostIdPtr, maxMessageChars) {
        var state = WebRemoteLinkState;
        var peer = state.createPeer(UTF8ToString(hostIdPtr), maxMessageChars);

        peer.on("open", function (id) {
            if (state.peer === peer) state.pushEvent("open", id, "");
        });
        peer.on("connection", function (connection) {
            if (state.peer === peer) state.watchConnection(connection);
        });
        peer.on("disconnected", function () {
            if (state.peer === peer) state.pushEvent("signaling", "", "lost");
        });

        //Connected remotes keep working without the signaling server, but new ones can not join until it is back.
        state.reconnectTimer = setInterval(function () {
            if (state.peer === peer && peer.disconnected && !peer.destroyed) {
                peer.reconnect();
            }
        }, state.RECONNECT_INTERVAL_MS);
    },

    //Connects this browser as a remote to a host. Reports "connected" with the host ID once messages can be sent.
    WebRemoteLink_Connect: function (hostIdPtr, maxMessageChars) {
        var state = WebRemoteLinkState;
        var hostId = UTF8ToString(hostIdPtr);
        var peer = state.createPeer(null, maxMessageChars); // remotes get a random ID from the server

        peer.on("open", function () {
            if (state.peer !== peer) return;
            state.watchConnection(peer.connect(hostId, { reliable: true, serialization: "raw" }));
        });
    },

    //Sends a string to a connected peer. Returns 1 if it was sent, 0 if the peer is not connected.
    WebRemoteLink_Send: function (peerIdPtr, textPtr) {
        var connection = WebRemoteLinkState.connections[UTF8ToString(peerIdPtr)];
        if (!connection || !connection.open) {
            return 0;
        }
        try {
            connection.send(UTF8ToString(textPtr));
        } catch (error) {
            return 0;
        }
        return 1;
    },

    //Closes the connection to one peer after the delay (so a last message can still go out).
    //The peer is forgotten right away: no more events arrive for it, also no "closed".
    WebRemoteLink_Close: function (peerIdPtr, delayMs) {
        var state = WebRemoteLinkState;
        var id = UTF8ToString(peerIdPtr);
        var connection = state.connections[id];
        if (!connection) {
            return;
        }
        delete state.connections[id];
        setTimeout(function () { connection.close(); }, delayMs);
    },

    //Closes everything and throws away the events that were not collected yet.
    WebRemoteLink_Stop: function () {
        WebRemoteLinkState.stop();
    },

    //Returns the oldest event that was not collected yet, or null if there is none.
    WebRemoteLink_PollEvent: function () {
        var state = WebRemoteLinkState;
        if (state.events.length === 0) {
            return 0;
        }
        var text = state.events.shift();
        var bufferSize = lengthBytesUTF8(text) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(text, buffer, bufferSize);
        return buffer;
    },

    //Returns the QR code for a text as rows of "1" (dark) and "0" (light), all rows in one string.
    //The code is square, so the side length is the square root of the string length. Null if it failed.
    WebRemoteLink_GetQrModules: function (textPtr) {
        if (!window.qrcode) {
            return 0;
        }
        var modules = "";
        try {
            var qr = window.qrcode(0, "M"); // 0 = pick the smallest size that fits
            qr.addData(UTF8ToString(textPtr));
            qr.make();
            var count = qr.getModuleCount();
            for (var row = 0; row < count; row++) {
                for (var column = 0; column < count; column++) {
                    modules += qr.isDark(row, column) ? "1" : "0";
                }
            }
        } catch (error) {
            return 0;
        }
        var bufferSize = lengthBytesUTF8(modules) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(modules, buffer, bufferSize);
        return buffer;
    },

    //Keeps the screen on while the page is visible. Does nothing in browsers without the Wake Lock API.
    WebRemoteLink_KeepScreenOn: function () {
        var state = WebRemoteLinkState;
        if (!navigator.wakeLock) {
            return;
        }
        var request = function () {
            navigator.wakeLock.request("screen").catch(function () { });
        };
        if (!state.wakeLockWanted) {
            state.wakeLockWanted = true;
            //The browser releases the lock when the page is hidden, so it is requested again when it is back.
            document.addEventListener("visibilitychange", function () {
                if (document.visibilityState === "visible") request();
            });
        }
        request();
    },

    //Copies a text to the clipboard. Some browsers refuse this outside of a click, then nothing happens.
    WebRemoteLink_CopyText: function (textPtr) {
        var text = UTF8ToString(textPtr);
        console.log("[WebRemoteLink] " + text);
        if (navigator.clipboard) {
            navigator.clipboard.writeText(text).catch(function () { });
        }
    }
};

autoAddDeps(WebRemoteLink, "$WebRemoteLinkState");
mergeInto(LibraryManager.library, WebRemoteLink);
