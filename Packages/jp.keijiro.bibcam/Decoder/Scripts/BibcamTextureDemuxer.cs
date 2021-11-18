using UnityEngine;
using Bibcam.Common;

namespace Bibcam.Decoder {

public sealed class BibcamTextureDemuxer : MonoBehaviour
{
    #region Hidden asset references

    [SerializeField, HideInInspector] Shader _shader = null;

    #endregion

    #region Public members

    public RenderTexture ColorTexture => _color;
    public RenderTexture DepthTexture => _depth;

    public void Demux(Texture source, in Metadata meta)
    {
        if(!_material)
            _material = new Material(_shader);
        
        // Lazy initialization for demuxing buffers
        var (w, h) = (source.width, source.height);
        if (_color == null) _color = GfxUtil.RGBARenderTexture(w / 2, h);
        if (_depth == null) _depth = GfxUtil.RHalfRenderTexture(w / 2, h / 2);

        // Demux shader invocations
        _material.SetVector(ShaderID.DepthRange, meta.DepthRange);
        Graphics.Blit(source, _color, _material, 0);
        Graphics.Blit(source, _depth, _material, 1);
    }

    #endregion

    #region Private members

    Material _material;
    RenderTexture _color;
    RenderTexture _depth;

    #endregion

    #region MonoBehaviour implementation

    void OnDestroy()
    {
        Destroy(_material);
        Destroy(_color);
        Destroy(_depth);
    }

    #endregion
}

} // namespace Bibcam.Decoder
