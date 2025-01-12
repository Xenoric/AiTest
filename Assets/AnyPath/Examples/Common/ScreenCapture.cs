using System.IO;
using UnityEngine;

namespace AnyPath.Examples
{
    public class ScreenCapture : MonoBehaviour
    {
        public string outputFolder;
        public int FPS = 30;
        
        private int captureFrame;
        private bool started;
        
        private void Awake()
        {
            if (string.IsNullOrEmpty(outputFolder))
            {
                Debug.LogError("CaptureCamera needs an output folder");
                Destroy(this);
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Debug.LogError("Please specifiy a valid output directory to store in");
                Destroy(this);
            }
        }

        private void OnEnable()
        {
            Time.captureFramerate = FPS;
        }

        private void OnDisable()
        {
            Time.captureFramerate = 0;
        }

        public void LateUpdate()
        {
            // first one is blank for some reason
            if (captureFrame > 0)
                UnityEngine.ScreenCapture.CaptureScreenshot(CapturePathToWrite(outputFolder, captureFrame));
            
            captureFrame++;
            Debug.Log("Saved: " + captureFrame);
        }

        private string CapturePathToWrite(string path, int frame)
        {
            return Path.Combine(path, frame.ToString("00000") + ".png");
        }
    }
}
