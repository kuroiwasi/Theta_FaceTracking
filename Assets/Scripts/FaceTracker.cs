using OpenCvSharp;
using System.Linq;
using UnityEngine;
using System;

public class FaceTracker : MonoBehaviour
{
    public GameObject ThetaSphere;
    public GameObject ViewQuad;
    public GameObject TrackingPoint;
    public Camera ThetaCamera;

    private  RenderTexture renderTex;
    private CascadeClassifier cascade;

    public enum THETA_DEVICE_TYPE
    {
        RICOH_THETA_V_FullHD,
        RICOH_THETA_V_4K,
        RICOH_THETA_S
    }

    [SerializeField] private THETA_DEVICE_TYPE deviceType;

    void Start ()
    {
        var deviceName = deviceType.ToString().Replace('_', ' ');

        WebCamDevice[] devices = WebCamTexture.devices;
        for (var i = 0; i < devices.Length; i++)
        {
            if (devices[i].name == deviceName)
            {
                Debug.Log(devices[i].name + " detected");
                WebCamTexture webCamTexture = new WebCamTexture(devices[i].name);
                ThetaSphere.GetComponent<Renderer>().material.mainTexture = webCamTexture;
                webCamTexture.Play();
                break;
            }
        }

        renderTex = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
        ViewQuad.GetComponent<Renderer>().material.mainTexture = renderTex;
        ThetaCamera.targetTexture = renderTex;

        cascade = new CascadeClassifier(Application.streamingAssetsPath + @"/haarcascade_frontalface_alt.xml");
    }

    void Update ()
    {
        var tex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.RGB24, false, false);
        RenderTexture.active = renderTex;
        tex.ReadPixels(new UnityEngine.Rect(0, 0, tex.width, tex.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        using (var image = new Mat(tex.height, tex.width, MatType.CV_8UC3, tex.GetRawTextureData()))
        {
            Cv2.CvtColor(image, image, ColorConversionCodes.BGR2RGB);
            Cv2.Flip(image, image, FlipMode.X);
            var faces = cascade.DetectMultiScale(image);
            var x = 0.0f;
            var y = 0.0f;

            if (faces.Length > 0)
            {
                var face = faces[
                    faces
                    .Select((val, idx) => new { V = Math.Abs(val.TopLeft.X - val.BottomRight.X), I = idx })
                    .Aggregate((max, working) => max.V > working.V ? max : working).I
                    ];
                x = face.TopLeft.X + face.Size.Width / 2;
                y = face.TopLeft.Y + face.Size.Height / 2;
                x = (x / renderTex.width - 0.5f) * 2;
                y = -(y / renderTex.height - 0.5f) * 2;
            }

            TrackingPoint.transform.position = new Vector3(x, y, 0);
            if (Math.Abs(y) > 0.15) ThetaCamera.transform.Rotate( -Math.Sign(y), 0, 0);
            if (Math.Abs(x) > 0.15) ThetaCamera.transform.Rotate( 0, Math.Sign(x), 0, Space.World);
        }
    }
}
