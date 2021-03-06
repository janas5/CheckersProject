﻿using System;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace CheckersApplication
{
    class Camera
    {
        public ImageViewer imageViewer;
        public Capture capture;
        
        public Camera(string path)
        {
            imageViewer = new ImageViewer();
            //0 parameters --> default camera; for DroidCam (Android phone)? --> capture = new Capture("http://IP:PORT/mjpegfeed"); 
            //"http://192.168.1.13:4747/mjpegfeed"
            if (path.Equals(""))
            {
                capture = new Capture();
            }
            else
            {
                try
                {
                    int i = Convert.ToInt16(path);
                    capture = new Capture(i);
                }
                catch
                {
                    capture = new Capture(path);
                }
            }
        }
    }
}
