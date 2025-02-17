using UnityEngine;
using UnityEngine.UI;
using Bibcam.Encoder;

sealed class BibcamController : MonoBehaviour
{
    #region Scene object references

    [SerializeField] BibcamEncoder _encoder = null;
    [SerializeField] Camera _camera = null;
    [SerializeField] RawImage _mainView = null;
    [SerializeField] GameObject _uiRoot = null;
    [SerializeField] Slider _depthSlider = null;
    [SerializeField] Text _depthLabel = null;

    #endregion

    #region Public members (exposed for UI)

    public void ToggleUI()
      => _uiRoot.SetActive(!_uiRoot.activeSelf);

    public void ResetOrigin()
      => _camera.transform.parent.position = -_camera.transform.localPosition;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        Application.targetFrameRate = 60;
        _mainView.texture = _encoder.EncodedTexture;
        _depthSlider.value = PlayerPrefs.GetFloat("DepthSlider", 5);
        _uiRoot.SetActive(false);
    }

    void Update()
    {
        var maxDepth = _depthSlider.value;
        var minDepth = maxDepth / 20;
        (_encoder.minDepth, _encoder.maxDepth) = (minDepth, maxDepth);
        _depthLabel.text = $"Depth Range: {minDepth:0.00} - {maxDepth:0.00}";
        PlayerPrefs.SetFloat("DepthSlider", maxDepth);
    }

    #endregion
}
