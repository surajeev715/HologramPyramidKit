using System.Collections.Generic;
using UnityEngine;

public class ObjectControlsUI
{
    public struct Result
    {
        public int PickedShape;
        public int PickedVariant;
        public bool Spinning;
        public bool WordPerLine;
    }

    public const string WORD_PER_LINE = "One word per line";

    private const int MIN_VARIANTS = 2;

    private readonly DropdownUI _shapes = new DropdownUI();
    private readonly DropdownUI _variants = new DropdownUI();

    public bool IsPicking => _shapes.IsOpen || _variants.IsOpen;

    public void CloseLists()
    {
        _shapes.Close();
        _variants.Close();
    }

    public Result Draw(UITheme theme, IReadOnlyList<string> shapeNames, int shapeIndex, string shapePlaceholder,
        IReadOnlyList<string> variantNames, int variantIndex, string variantPlaceholder, bool spinning,
        bool? wordPerLine, float listSpace)
    {
        bool hasVariants = variantNames != null && variantNames.Count >= MIN_VARIANTS;
        bool isPickingShape = _shapes.IsOpen;
        bool isPickingVariant = _variants.IsOpen && hasVariants;
        if (!hasVariants)
        {
            _variants.Close();
        }

        Result result = new Result
        {
            PickedShape = RemoteProtocol.NO_INDEX,
            PickedVariant = RemoteProtocol.NO_INDEX,
            Spinning = spinning,
            WordPerLine = wordPerLine ?? false
        };
        result.PickedShape = _shapes.Draw(theme, "Object", shapeNames, shapeIndex, shapePlaceholder, listSpace);

        if (_shapes.IsOpen && !isPickingShape)
        {
            _variants.Close();
        }
        if (isPickingShape)
        {
            return result;
        }

        if (hasVariants)
        {
            GUILayout.Space(theme.Gap);
            float variantListSpace = listSpace - theme.Gap - theme.Row;
            result.PickedVariant = _variants.Draw(theme, "Variant", variantNames, variantIndex, variantPlaceholder, variantListSpace);
            if (_variants.IsOpen && !isPickingVariant)
            {
                _shapes.Close();
            }
        }
        if (isPickingVariant)
        {
            return result;
        }

        GUILayout.Space(theme.Gap);
        result.Spinning = theme.SwitchRow("Spin", spinning);

        if (wordPerLine.HasValue)
        {
            GUILayout.Space(theme.Gap);
            result.WordPerLine = theme.SwitchRow(WORD_PER_LINE, wordPerLine.Value);
        }
        return result;
    }

    public static float Rows(bool hasWordPerLineRow)
    {
        float rows = 1f + UITheme.GAP + 1f + UITheme.GAP + 1f;
        return hasWordPerLineRow ? rows + UITheme.GAP + 1f : rows;
    }
}
