using UnityEngine;
using System;

class CandleAnimationScript : MonoBehaviour
{
    private Animator _candleAnimation;

    private bool _isCandleBurning;

    void Start()
    {
        Debug.Log("[CANDLE] Started with Off");
        _candleAnimation = GetComponentInChildren<Animator>();
        if (_candleAnimation == null)
            Debug.LogError("[CANDLE] No Animation component found!");
        _candleAnimation.Play("Candle_Rig|Candle_Off");
        _isCandleBurning = false;
    }

    public void Animate(bool hasLight, bool hasBlow)
    {
        if (hasLight || hasBlow)
            Debug.Log($"[CANDLE] Animate: light={hasLight}, blow={hasBlow}, burning={_isCandleBurning}");

        if (_isCandleBurning)
        {
            handleBurningAndExtinguishState(hasLight, hasBlow);
        }
        else
        {
            handleOffState(hasLight, hasBlow);
        }

    }

    public void handleOffState(bool hasLight, bool hasBlow)
    {
        if (hasLight && !hasBlow) {
            _isCandleBurning = true;
            Debug.Log("[CANDLE] LIGHT -> Candle is Now Burning");
            _candleAnimation.Play("Candle_Rig|Candle_Burning");
        }
    }

    public void handleBurningAndExtinguishState(bool hasLight, bool hasBlow)
    {
        if (hasBlow) {
            _isCandleBurning = false;
            Debug.Log("[CANDLE] BLOW -> Candle OFF");
            _candleAnimation.Play("Candle_Rig|Candle_Extinguish");
            _candleAnimation.Play("Candle_Rig|Candle_Off");
        }
    }
}
