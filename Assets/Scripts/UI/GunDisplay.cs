using UnityEngine;
using UnityEngine.UI;

public class GunDisplay : MonoBehaviour
{
    public Text BulleteNumText;

    public Image[] GunAccessory;//0为弹夹，1为射线，2为倍镜，3为握把

    public void SetBulletCount(int currentAmmo)
    {
        if (BulleteNumText != null)
            BulleteNumText.text = $"{currentAmmo}/∞";
    }

    public void SetGunAccessoryVisible(bool[] visible)
    {
        if (GunAccessory == null || visible == null) return;

        int count = Mathf.Min(Mathf.Min(GunAccessory.Length, visible.Length), 4);
        for (int i = 0; i < count; i++)
        {
            if (GunAccessory[i] != null)
                GunAccessory[i].gameObject.SetActive(visible[i]);
        }
    }
}
