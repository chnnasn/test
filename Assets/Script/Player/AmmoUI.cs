using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _ammoText;
    [SerializeField] private TMP_Text _reloadingText;

    public void Refresh(int currentAmmo, int magazineSize, bool isReloading)
    {
        if (_ammoText == null) return;
        _reloadingText.text = isReloading ? "换弹中..." : "";
        _ammoText.text = isReloading ? "" : $"{currentAmmo}/{magazineSize}";
    }
}
