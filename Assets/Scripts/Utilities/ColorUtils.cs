using UnityEngine;

public static class ColorUtils
{
    public static Color GetColorByIndex(int colorIndex)
    {
        switch (colorIndex)
        {
            case 0:
                return Color.white;
            case 1:
                return Color.red;
            case 2:
                return  new Color(1f, 0.647f, 0f);
            case 3:
                return Color.yellow;
            case 4:
                return Color.green;
            case 5:
                return Color.blue;
            case 6:
                return new Color(0.502f, 0f, 0.502f); 
        }
        return Color.white;
    }
 
}
