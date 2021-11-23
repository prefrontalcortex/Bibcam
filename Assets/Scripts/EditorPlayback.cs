using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bibcam;
using Bibcam.Decoder;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

[ExecuteAlways]
public class EditorPlayback : MonoBehaviour
{
    [Range(0,1)]
    public float time;

    private BibcamVideoFeeder feeder;
    private BibcamCameraController camController;
    
    [SerializeField]
    private BibcamPointCloud pointCloud;

    private void OnEnable()
    {
        var v = GetComponent<VideoPlayer>();
        v.seekCompleted -= UpdateScene;
        v.seekCompleted += UpdateScene;
        v.playbackSpeed = 1;
        isSeeking = false;
    }

    private void OnDisable()
    {
        var v = GetComponent<VideoPlayer>();
        v.seekCompleted -= UpdateScene;
    }

    private void OnValidate()
    {
        var v = GetComponent<VideoPlayer>();
        Seek(v.length * time);
    }

    private bool isSeeking = false;
    private void Seek(double time)
    {
        if (overridePlay) return;
        if (isSeeking) return;
        isSeeking = true;
        var v = GetComponent<VideoPlayer>();
        v.time = time;
        v.playbackSpeed = 0;
        v.Play();
    }

    private void UpdateScene(VideoPlayer source) 
    {
        if (!source) return;

        if (!feeder && !TryGetComponent(out feeder)) return;
        var cam = Camera.main;
        if (!cam) return;
        if (!camController && !cam.TryGetComponent(out camController)) return;
        
        // update metadata decoder and texture demuxer
        feeder.Update();
        camController.LateUpdate();

        // repaint scene view
        SceneView.RepaintAll();

        isSeeking = false;
        if (!Application.isPlaying && !overridePlay && Mathf.Abs((float)source.time - (float)source.length * time) > 0.05f) {
            EditorApplication.delayCall += () =>
            Seek(source.length * time);
        }
    }

    private void OnDrawGizmos()
    {
        var c = Camera.main;
        Gizmos.matrix = c.transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, c.fieldOfView, c.nearClipPlane, c.farClipPlane, c.aspect);

        Gizmos.matrix = Matrix4x4.identity;
        if(samples != null)
        {
            Gizmos.color = new Color(0.15f, 0.7f, 0.5f);
            for (int i = 1; i < samples.Count; i++)
            {
                var mat0 = samples[i - 1].MultiplyPoint(Vector3.zero);
                var mat1 = samples[i].MultiplyPoint(Vector3.zero);
                Gizmos.DrawLine(mat0, mat1);
            }
            
            Gizmos.color = new Color(1, 1, 1, 0.1f);
            for (int i = 0; i < samples.Count; i++)
            {
                Gizmos.matrix = samples[i];
                Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.333f);
            }
        }
    }

    private bool overridePlay = false;
    // [ContextMenu("Play")]
    // void PlayBack()
    // {
    //     overridePlay = true;
    //     var v = GetComponent<VideoPlayer>();
    //     v.time = 0;
    //     v.playbackSpeed = 1;
    //     v.Play();
    //     v.frameReady -= FrameReady;
    //     v.frameReady += FrameReady;
    // }
    //
    // [ContextMenu("Stop")]
    // void Stop()
    // {
    //     overridePlay = false;
    // }

    private void FrameReady(VideoPlayer source, long frameidx)
    { 
        SceneView.RepaintAll();
        source.frameReady -= FrameReady;
    }

    [ContextMenu("Sample")]
    private async void Sample()
    {
        samples.Clear();
        
        var v = GetComponent<VideoPlayer>();
        var l = v.length;
        
        // take 100 samples
        var timestep = l / 100.0;

        v.seekCompleted -= UpdateScene;
        v.seekCompleted += SampleTaken;
        
        if(pointCloud)
            pointCloud.BeginSample();
        
        var sampleTime = 0.0;
        while (sampleTime < l)
        {
            sampleHasBeenTaken = false;
            v.time = sampleTime;
            while (!sampleHasBeenTaken) await Task.Delay(10);

            if(Mathf.Abs((float)v.time - (float) sampleTime) < 0.1f)
                samples.Add(Camera.main.transform.localToWorldMatrix); 
            
            if(pointCloud)
                pointCloud.SampleFrame();
            
            // seek time and wait for seek to be finished
            sampleTime += timestep;
        }
        if(pointCloud)
            pointCloud.EndSample();

        sampleHasBeenTaken = false;
        v.time = 0;
        while (!sampleHasBeenTaken) await Task.Delay(10);
        v.seekCompleted -= SampleTaken;
        

        OnDisable();
        OnEnable();
    }

    private List<Matrix4x4> samples = new List<Matrix4x4>();
    private bool sampleHasBeenTaken;
    private void SampleTaken(VideoPlayer source)
    {   
        GetComponent<BibcamVideoFeeder>().Update();
        Camera.main.GetComponent<BibcamCameraController>().LateUpdate();
        
        sampleHasBeenTaken = true;
    }
}
