using UnityEngine;
#if BIBCAM_HAS_UNITY_VIDEO
using UnityEngine.Video;
#endif

namespace Bibcam.Decoder {

public sealed class BibcamVideoFeeder : MonoBehaviour
{
#if BIBCAM_HAS_UNITY_VIDEO

    #region Scene object reference

    [SerializeField] BibcamMetadataDecoder _decoder = null;
    [SerializeField] BibcamTextureDemuxer _demuxer = null;

    #endregion

    #region Private objects

    RenderTexture _delay;

    #endregion

    #region MonoBehaviour implementation
    void OnDestroy()
      => Destroy(_delay);

    public void Update()
    {
    	if(!_delay) _delay = new RenderTexture(1920, 1080, 0);
        var video = GetComponent<VideoPlayer>();
        if (video.texture == null) return;
        _decoder.Decode(video.texture);
        _demuxer.Demux(_delay, _decoder.Metadata);
        Graphics.Blit(video.texture, _delay);
    }

    #endregion

#else

    void OnValidate()
      => Debug.LogError("UnityEngine.Video is missing.");

#endif
}

} // namespace Bibcam.Decoder
