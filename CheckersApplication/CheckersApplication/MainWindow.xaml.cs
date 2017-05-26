﻿using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;
using System.Collections.ObjectModel;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Features2D;
using Emgu.CV.CvEnum;

using DirectShowLib;

namespace CheckersApplication
{
    /// <summary>
    /// Interaction logic for CameraCapture.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Camera camera;
        ChessBoardState chessBoardState = new ChessBoardState();

        public MainWindow()
        {
            InitializeComponent();            
            DiscoverUsbCameras();
            CvInvoke.UseOpenCL = (bool)CB_OpenCL.IsChecked;
            this.ChessBoard.ItemsSource = chessBoardState.pieces;
        }

        public void DiscoverUsbCameras()
        {
            List<KeyValuePair<int, string>> ListCamerasData = new List<KeyValuePair<int, string>>();
            //-> Find systems cameras with DirectShow.Net dll
            DsDevice[] _SystemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            int _DeviceIndex = 0;
            foreach (DirectShowLib.DsDevice _Camera in _SystemCamereas)
            {
                CO_Cameras.Items.Add(new KeyValuePair<int, string>(_DeviceIndex, _Camera.Name));
                _DeviceIndex++;
            }
            if (CO_Cameras.Items.Count > 0)
                CO_Cameras.SelectedIndex = 0;
        }
                
        public void updateFrames(object sender, EventArgs e)
        {
            try
            {
                camera.imageViewer.Image = camera.capture.QueryFrame();
                var image = new Image<Bgr, Byte>(camera.imageViewer.Image.Bitmap);
                Detect(cameraCapture: image);
            }
            catch (Exception ex) {
                ComponentDispatcher.ThreadIdle -= new EventHandler(updateFrames);
                System.Windows.MessageBox.Show(ex.Message);
                ChangeBtnStartStop();
            }
        }

        private void ChangeBtnStartStop()
        {
            BT_Start.IsEnabled = !BT_Start.IsEnabled;
            BT_Stop.IsEnabled = !BT_Stop.IsEnabled;
        }

        public void CameraShow()
        {
            ComponentDispatcher.ThreadIdle += new System.EventHandler(updateFrames);
        }

        private void BT_Start_Click(object sender, RoutedEventArgs e)
        {
            ChangeBtnStartStop();
            if (CB_DefaultCamera.IsChecked==true && CO_Cameras.Items.Count>0)     
                camera = new Camera(Convert.ToString(CO_Cameras.SelectedIndex));
            else
                camera = new Camera(TB_CameraSource.Text);
            CameraShow();
            CO_Cameras.IsEnabled = false;
        }

        private void BT_Stop_Click(object sender, RoutedEventArgs e)
        {
            ChangeBtnStartStop();
            ComponentDispatcher.ThreadIdle -= new EventHandler(updateFrames);
            camera.capture.Stop();
            camera.capture.Dispose();
            CO_Cameras.IsEnabled = true;
        }

        private void CB_DefaultCamera_Click(object sender, RoutedEventArgs e)
        {
            CO_Cameras.IsEnabled = !CO_Cameras.IsEnabled;
            TB_CameraSource.IsEnabled = !(bool)CB_DefaultCamera.IsChecked;
            if (!TB_CameraSource.IsEnabled)
            {
                TB_CameraSource.Text = "URL streamu, ID kamery, lub plik wideo,\nnp. (http://IP:PORT/mjpegfeed)";
                TB_CameraSource.Foreground = System.Windows.Media.Brushes.Gray;
            }           
        }

        private void CB_OpenCL_Click(object sender, RoutedEventArgs e)
        {
            CvInvoke.UseOpenCL = !CvInvoke.UseOpenCL;
        }

        private void BT_ImageTest_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Image files (*.jpg, *.png) | *.jpg; *.png";
            openFileDialog1.RestoreDirectory = true;
            DialogResult result = openFileDialog1.ShowDialog();
            string path = String.Empty;
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    Detect(openFileDialog1.FileName);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Komunikat błędu: " + ex.Message);
                }
            }
        }

        private void TB_CameraSource_GotFocus(object sender, RoutedEventArgs e)
        {
            TB_CameraSource.Text = "";
            TB_CameraSource.Foreground = System.Windows.Media.Brushes.Black;
        }

        private void Detect(string filePath = null, Image<Bgr, byte> cameraCapture = null)
        {
            Image<Bgr, byte> image, resultImage;
            List<Color> currentColors = new List<Color>();
            System.Windows.Media.Color color = new System.Windows.Media.Color();
            System.Windows.Media.Color player1Color = new System.Windows.Media.Color();
            System.Windows.Media.Color player2Color = new System.Windows.Media.Color();
            bool player1Detected = false;
            bool player2Detected = false;


            if (filePath != null)
                image = new Image<Bgr, byte>(filePath).Resize(400, 400, Inter.Linear, true);
            else
                image = cameraCapture;

            resultImage = image.Copy();
            System.Drawing.Point[]points = Detection.GetRectanglePoints(image);

            if (points != null)
            {
                resultImage.Draw(points, new Bgr(Color.DarkOrange), 2);
                ChessField[,] fields = ChessField.GetChessFields(points);
                if (fields != null)
                {
                    foreach (var field in fields)
                        resultImage.Draw(field.points, new Bgr(Color.Green), 2);

                    CircleF[] circles = Detection.GetCircles(image);

                    if (circles != null)
                    {
                        foreach (CircleF circle in circles)
                        {
                            resultImage.Draw(circle, new Bgr(Color.Blue), 3);
                            try
                            {
                                currentColors.Clear();
                                for (int i = 0; i < circle.Radius-2; i++)
                                {
                                    currentColors.Add(image.Bitmap.GetPixel((int)circle.Center.X + i, (int)circle.Center.Y));
                                    currentColors.Add(image.Bitmap.GetPixel((int)circle.Center.X - i, (int)circle.Center.Y));
                                    currentColors.Add(image.Bitmap.GetPixel((int)circle.Center.X, (int)circle.Center.Y+i));
                                    currentColors.Add(image.Bitmap.GetPixel((int)circle.Center.X, (int)circle.Center.Y-i));
                                    currentColors.Add(image.Bitmap.GetPixel((int)circle.Center.X + i, (int)circle.Center.Y+i));
                                    currentColors.Add(image.Bitmap.GetPixel((int)circle.Center.X - i, (int)circle.Center.Y-i));
                                    currentColors.Add(image.Bitmap.GetPixel((int)circle.Center.X + i, (int)circle.Center.Y-i));
                                    currentColors.Add(image.Bitmap.GetPixel((int)circle.Center.X - i, (int)circle.Center.Y+i));
                                }


                                int r = 0, g = 0, b = 0, counter = 0;
                                foreach (Color clr in currentColors)
                                {
                                    counter++;
                                    r += (int)clr.R;
                                    g += (int)clr.G;
                                    b += (int)clr.B;
                                }
                                r = r / counter;
                                g = g / counter;
                                b = b / counter;
                                CircleF c = new CircleF(circle.Center, 5);
                                resultImage.Draw(c, new Bgr(Color.Yellow), 3);
                                color = System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b);

                                if (player1Detected==false)
                                {
                                    player1Color = color;
                                    player1Detected = true;
                                }

                                else if (player2Detected == false)
                                {
                                    player2Color = color;
                                    player2Detected = true;
                                }

                            }
                            catch(Exception e)
                            {

                            }
                        }

                        ChessField.Pons(fields, circles);
                        chessBoardState.Clear();
                        chessBoardState.AddPieces(fields);
                    }
                }
            }
            IMG_Detected.Source = ToBitmapConverter.Convert(resultImage);
            CV_Player1Color.Background = new System.Windows.Media.SolidColorBrush(player1Color);
            CV_Player2Color.Background = new System.Windows.Media.SolidColorBrush(player2Color);
        }

    }
}
