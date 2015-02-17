using FaceDetector;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Devices;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Xml.Linq;

namespace Trollfie.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {

        PhotoCamera camera = new PhotoCamera();

        const string MODEL_FILE = "FaceModel/haarcascade_frontalface_alt.xml";
        FaceDetector.Detector _detector;
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        /// 
        /// <summary>
        /// The <see cref="CaptureImageCommand" /> property's name.
        /// </summary>
        public const string CaptureImageCommandPropertyName = "CaptureImageCommand";

        private RelayCommand _captureImageCommand = null;

        public RelayCommand CaptureImageCommand
        {
            get
            {
                return _captureImageCommand;
            }

            set
            {
                if (_captureImageCommand == value)
                {
                    return;
                }

                _captureImageCommand = value;
                RaisePropertyChanged(CaptureImageCommandPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="ProcessCommand" /> property's name.
        /// </summary>
        public const string ProcessCommandPropertyName = "ProcessCommand";

        private RelayCommand _ProcessCommand = null;
        public RelayCommand ProcessCommand
        {
            get
            {
                return _ProcessCommand;
            }

            set
            {
                if (_ProcessCommand == value)
                {
                    return;
                }

                _ProcessCommand = value;
                RaisePropertyChanged(ProcessCommandPropertyName);
            }
        }





        /// <summary>
        /// The <see cref="ViewImageSource" /> property's name.
        /// </summary>
        public const string ViewImageSourcePropertyName = "ViewImageSource";

        private ImageSource _ViewImageSource = null;

        public ImageSource ViewImageSource
        {
            get
            {
                return _ViewImageSource;
            }

            set
            {
                

                _ViewImageSource = value;
                RaisePropertyChanged(ViewImageSourcePropertyName);
            }
        }

        private WriteableBitmap EditBitmap;
        

        
        

        public MainViewModel()
        {
            _detector = new FaceDetector.Detector(XDocument.Load(MODEL_FILE));


            CaptureImageCommand = new RelayCommand(CaptureImageAction);
            ProcessCommand = new RelayCommand(ProcessImageAction, () => { return ViewImageSource != null; });

            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
        }

        private void ProcessImageAction()
        {
            WriteableBitmap SourceWritableBitmap = EditBitmap;
            //SourceWritableBitmap = DownsampleImage(SourceWritableBitmap);
           
            ViewImageSource = SourceWritableBitmap;

           // SourceWritableBitmap = ResizeImage(SourceWritableBitmap);

            var faces = DetectFaces(SourceWritableBitmap);
            SourceWritableBitmap = MaskFacesWithTrollFace(SourceWritableBitmap, faces);
            //ViewImageSource = null;
            ViewImageSource = SourceWritableBitmap;
            

            
        }

        private WriteableBitmap ResizeImage(WriteableBitmap wbitmap)
        {
            var rWidth = 640.0d;
            var rHeight = rWidth / (double)wbitmap.PixelWidth * (double)wbitmap.PixelHeight;

            return wbitmap.Resize((int)rWidth, (int)rHeight, WriteableBitmapExtensions.Interpolation.Bilinear);

        }

        private WriteableBitmap ResizeImage(BitmapImage bitmap)
        {
            WriteableBitmap wbmp = new WriteableBitmap(bitmap);
            return ResizeImage(wbmp);

        }

        private void CaptureImageAction()
        {
            PhotoChooserTask photo = new PhotoChooserTask();
            photo.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
            photo.ShowCamera = true;
            photo.Show();
        }

        private void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                BitmapImage SourceBitmap = new BitmapImage();
                
                SourceBitmap.SetSource(e.ChosenPhoto);


                EditBitmap = ResizeImage(SourceBitmap);

                ViewImageSource =EditBitmap;

                this.ProcessCommand.RaiseCanExecuteChanged();

                
            }
        }

        public WriteableBitmap DownsampleImage( WriteableBitmap SourceWritableBitmap, int _downsampleFactor=2)
        {
            byte[] downsampledImage = new byte[SourceWritableBitmap.PixelWidth / _downsampleFactor * SourceWritableBitmap.PixelHeight / _downsampleFactor];

            Utils.DownSample(SourceWritableBitmap.ToByteArray(), SourceWritableBitmap.PixelWidth, SourceWritableBitmap.PixelHeight, ref downsampledImage, _downsampleFactor);

            SourceWritableBitmap = SourceWritableBitmap.FromByteArray(downsampledImage);

            return SourceWritableBitmap;
            
        }

        private List<Rectangle> DetectFaces(WriteableBitmap SourceWritableBitmap)
        {
            List<FaceDetector.Rectangle> faces = new List<FaceDetector.Rectangle>();
            
            faces = _detector.getFaces(SourceWritableBitmap, 2f, 1.25f, 0.1f, 1, false, true);           
            //facesPic.Source = SourceWritableBitmap;
            return faces;
        }

        private  WriteableBitmap MaskFacesWithTrollFace(WriteableBitmap SourceWritableBitmap, List<FaceDetector.Rectangle> faces)
        {
           

            StreamResourceInfo MaskImageSri = Application.GetResourceStream(new Uri("Images/Troll.png", UriKind.Relative));
            BitmapImage MaskImageBitmap = new BitmapImage();
            MaskImageBitmap.SetSource(MaskImageSri.Stream);

            WriteableBitmap MaskWritableBitmap = new WriteableBitmap(MaskImageBitmap);

            foreach (var r in faces)
            {
                int x = Convert.ToInt32(r.X);
                int y = Convert.ToInt32(r.Y);
                int width = Convert.ToInt32(r.Width);
                int height = Convert.ToInt32(r.Height);

                int offset = 20;


                System.Windows.Rect destRect = new Rect(x-offset, y-offset, width+offset+10, height+offset+10);
                System.Windows.Rect srcRect = new Rect(0, 0, MaskWritableBitmap.PixelWidth, MaskWritableBitmap.PixelHeight);

                SourceWritableBitmap.Blit(destRect, MaskWritableBitmap, srcRect);

            }
            if(faces.Count>0)
            SourceWritableBitmap.Invalidate();
             
            return SourceWritableBitmap;
        }
    }
}