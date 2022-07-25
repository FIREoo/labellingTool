using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace wpf_labelling_tool
{
    public partial class MainWindow : Window
    {
        Mat mat_show = new Mat();//= new Mat(480, 640, DepthType.Cv8U, 3);

        VideoCapture webCam;
        Mat mat_cam = new Mat(480, 640, DepthType.Cv8U, 3);
        bool liveLoop = false;

        Mat mat_LoadImage;//= new Mat(480, 640, DepthType.Cv8U, 3);

        Mat mat_boundBox_tmp;// = new Mat(480, 640, DepthType.Cv8U, 3);
        System.Drawing.Size loadImageSize = new System.Drawing.Size();

        static ObservableCollection<ListViewData> ListViewDataCollection = new ObservableCollection<ListViewData>();

        enum ShowMode
        {
            none = 0,
            camera = 1,
            video = 2,
            file = 3
        }
        ShowMode Mode = ShowMode.none;

        double RealScale = 1;

        public MainWindow()
        {
            InitializeComponent();
            lv_tranningList.ItemsSource = ListViewDataCollection;

            Mat black = new Mat(480, 640, DepthType.Cv8U, 3);
            setToZero(ref black);
            img_main.Source = BitmapSourceConvert.MatToBitmap(black);

            //combobox resolution
            List<string> resolution_items = new List<string>();
            resolution_items.Add("320x240");
            resolution_items.Add("640x480");
            resolution_items.Add("960x720");
            resolution_items.Add("1280x960");
            resolution_items.Add("1600x1200");
            combo_resolution.ItemsSource = resolution_items;
            combo_resolution.Text = "1280x960";
        }
        #region unUse

        //public void timeCycle(object sender, EventArgs e)
        //{
        //    if (Mode == ShowMode.none)
        //    {

        //    }
        //    else if (Mode == ShowMode.camera)
        //    {

        //    }
        //    else if (Mode == ShowMode.file)
        //    {
        //        mat_LoadImage.CopyTo(mat_show);
        //        mat_show.AddLayer(mat_guideLine);
        //        img_main.Source = BitmapSourceConvert.MatToBitmap(mat_show);
        //        Console.WriteLine("tick");
        //    }
        //}  
        //private void ImageUpdate(bool alreadyHandle)
        //{
        //    if (alreadyHandle == false)
        //    {
        //        if (mat_show == null || mat_LoadImage == null)
        //            return;
        //        mat_LoadImage.CopyTo(mat_show);
        //        mat_show.AddLayer(mat_boundBox);
        //        mat_show.AddLayer(mat_boundBox_tmp);
        //        mat_show.AddLayer(mat_guideLine);
        //        img_main.Source = BitmapSourceConvert.MatToBitmap(mat_show);
        //    }

        //}

        //Capture
        #endregion unUse
   
        private void rb_cam_scale_Click(object sender, RoutedEventArgs e)
        {
            RadioButton cb = (RadioButton)sender;
            List<string> resolution_items = new List<string>();
            if ((string)cb.Content == "4:3")
            {
                resolution_items.Add("320x240");
                resolution_items.Add("640x480");
                resolution_items.Add("960x720");
                resolution_items.Add("1280x960");
                resolution_items.Add("1600x1200");
                combo_resolution.Text = "1280x960";
            }
            else if ((string)cb.Content == "16:9")
            {
                resolution_items.Add("640x360");
                resolution_items.Add("960x540");
                resolution_items.Add("1280x720");
                resolution_items.Add("1600x900");
                resolution_items.Add("1920x1080");
                combo_resolution.Text = "1280x720";
            }
            combo_resolution.ItemsSource = resolution_items;
        }
        private void Btn_openCamera_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(tb_open_index.Text);
            webCam = new VideoCapture(index);
            string[] res = combo_resolution.Text.Split('x');
            webCam.Set(CapProp.FrameWidth, int.Parse(res[0]));
            webCam.Set(CapProp.FrameHeight, int.Parse(res[1]));
            liveLoop = true;
            if (int.Parse(res[0]) == webCam.Width && int.Parse(res[1]) == webCam.Height)
                MessageBox.Show($"Camera Capture:{webCam.Width}x{webCam.Height}");
            else
            {
                MessageBox.Show($"Error resolution");
                webCam.Dispose();
                return;
            }

            int si = 0;//4:3
            RealScale = webCam.Width / 640.0; //反正4:3 16:9寬都是640
            tb_show_scale.Text = "Real scale: x" + RealScale;
            if (rb_cam_scale_169.IsChecked == true)
            {
                si = 1;
            }
            else if (rb_cam_scale_43.IsChecked == true)
            {
                si = 0;
            }

            Mode = ShowMode.camera;
            Task.Run(() =>
            {
                Mode = ShowMode.camera;
                while (liveLoop)
                {
                    mat_cam = webCam.QueryFrame();
                    if (si == 0)
                    {
                        CvInvoke.Resize(mat_cam, mat_show, new System.Drawing.Size(640, 480));//4:3
                    }
                    else if (si == 1)
                    {
                        CvInvoke.Resize(mat_cam, mat_show, new System.Drawing.Size(640, 360));//16:9
                        Mat blank;
                        blank = Mat.Zeros(120, 640, DepthType.Cv8U, 3);
                        CvInvoke.VConcat(mat_show, blank, mat_show);
                    }
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        img_main.Source = BitmapSourceConvert.MatToBitmap(mat_show);
                    }));
                }
            });
        }
        private void Btn_stop_cam_Click(object sender, RoutedEventArgs e)
        {
            liveLoop = false;
            Mat black = new Mat(480, 640, DepthType.Cv8U, 3);
            setToZero(ref black);
            img_main.Source = BitmapSourceConvert.MatToBitmap(black);
            Mode = ShowMode.none;
        }
        private void Btn_saveImage_Click(object sender, RoutedEventArgs e)
        {
            //確認有沒有資料夾
            string folder = tb_saveImage_folder.Text;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            Mat mat_save = new Mat();
            mat_cam.CopyTo(mat_save);
            if (cb_threadShow.IsChecked == true)
            {
                CvInvoke.Resize(mat_save, mat_save, new System.Drawing.Size(int.Parse(tb_saveSize_width.Text), int.Parse(tb_saveSize_height.Text)));
            }
            CvInvoke.Imwrite($"{folder}//{tb_saveImage_pretext.Text}{tb_saveImage_count.Text}.png", mat_save);
            tb_saveImage_count.Text = (int.Parse(tb_saveImage_count.Text) + 1).ToString();
        }
        private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex("[^0-9.-]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        string OpenFolder = "";
        FileInfo[] OpenFiles;
        int OpenFilesIndex = 0;
        List<LabellingInfo> LabelList = new List<LabellingInfo>();

        //---Load---//
        private void Btn_loadFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog path = new System.Windows.Forms.FolderBrowserDialog();
            path.ShowDialog();

            OpenFolder = path.SelectedPath;
            LoadFolder();
        }
        private void Btn_loadFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Count() > 1)
                {
                    MessageBox.Show("one file only");
                    return;
                }

                OpenFolder = files[0];
                LoadFolder();
            }
        }
        void LoadFolder()
        {
            if (OpenFolder == "")
            {
                Mode = ShowMode.none;
                return;
            }
            Mode = ShowMode.file;
            lb_selectLoadFolder.Content = $"Load Path : " + OpenFolder;

            DirectoryInfo OpenDirectory = new DirectoryInfo(OpenFolder);//Assuming Test is your Folder
            OpenFiles = OpenDirectory.GetFiles("*.png"); //Getting Text files
            if (OpenFiles.Count() == 0)
            {
                MessageBox.Show("沒有png");
                lb_loadImage.Content = "Load Image : N/A";
                return;
            }
            liveLoop = false;

            //zero something
            OpenFilesIndex = 0;

            Btn_reLoadImage_Click(null, null);
        }

        //---next image---//
        private void Btn_nextImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFilesIndex++;
            if (OpenFiles.Count() == OpenFilesIndex)
            {
                MessageBox.Show("Last image");
                OpenFilesIndex--;
                return;
            }
            Btn_reLoadImage_Click(sender, e);
        }
        private void Btn_previousImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFilesIndex--;
            if (OpenFilesIndex < 0)
            {
                MessageBox.Show("First image");
                OpenFilesIndex++;
                return;
            }
            Btn_reLoadImage_Click(sender, e);
        }
        private void Btn_reLoadImage_Click(object sender, RoutedEventArgs e)
        {
            boundtype = BoundBox.none;

            //clear all label list
            LabelList = new List<LabellingInfo>();
            ClearAllShowedRect();

            //load and show image
            lb_loadImage.Content = $"Load Image({OpenFilesIndex + 1}/{OpenFiles.Count()}) : " + OpenFiles[OpenFilesIndex].Name;
            mat_LoadImage = CvInvoke.Imread(OpenFiles[OpenFilesIndex].FullName);
            loadImageSize = mat_LoadImage.Size;
            img_main.Source = BitmapSourceConvert.MatToBitmap(mat_LoadImage);

            //scaling problem.
            RealScale = loadImageSize.Width / 640.0; //反正4:3 16:9寬都是640
            tb_show_scale.Text = "Real scale: x" + RealScale;


            if (cb_loadOldLabel.IsChecked == true)//如果要讀已經lebel過的檔案
            {
                string fileName = OpenFiles[OpenFilesIndex].FullName.Replace(".png", ".txt");
                if (System.IO.File.Exists(fileName) == true)
                {
                    string[] allString = System.IO.File.ReadAllLines(fileName);
                    for (int line = 0; line < allString.Count(); line++)
                    {
                        int index = allString[line].Split(' ')[0].ToInt();
                        double x_center = allString[line].Split(' ')[1].ToDouble();
                        double y_center = allString[line].Split(' ')[2].ToDouble();
                        double width = allString[line].Split(' ')[3].ToDouble();
                        double height = allString[line].Split(' ')[4].ToDouble();

                        Rectangle rect = new Rectangle();
                        rect.Width = (int)(width * loadImageSize.Width / RealScale);
                        rect.Height = (int)(height * loadImageSize.Height / RealScale);
                        rect.X = (int)((x_center * loadImageSize.Width/ RealScale) - (rect.Width / 2) );
                        rect.Y = (int)((y_center * loadImageSize.Height / RealScale) - (rect.Height / 2));
                        LabelList.Add(new LabellingInfo(index, rect));
                    }
                    ReShowLabellingRect();
                }
            }
            grid_save_notice.Background = new SolidColorBrush(Color.FromRgb(255, 100, 100));//unsave
        }
        private void Btn_saveLabelling_Click(object sender, RoutedEventArgs e)
        {
            if (boundtype == BoundBox.done || boundtype == BoundBox.none)
            {
                string name = OpenFiles[OpenFilesIndex].Name.Substring(0, OpenFiles[OpenFilesIndex].Name.IndexOf("."));
                StreamWriter txt = new StreamWriter($"{OpenFolder}//{name}.txt", false);
                foreach (LabellingInfo info in LabelList)
                {
                    if (info.Index >= 0)
                    {
                        //<x_center> <y_center> <width> <height> - float values relative to width and height of image, it can be equal from (0.0 to 1.0]
                        double x_center = (info.boundingbox.X + info.boundingbox.Width / 2.0) * RealScale / (double)loadImageSize.Width;
                        if (x_center > 1) x_center = 1; if (x_center < 0) x_center = 0.0001;//防止超過邊線(0,1]
                        double y_center = (info.boundingbox.Y + info.boundingbox.Height / 2.0) * RealScale / (double)loadImageSize.Height;
                        if (y_center > 1) y_center = 1; if (y_center < 0) y_center = 0.0001;//防止超過邊線(0,1]
                        double width = Math.Abs(((double)info.boundingbox.Width * RealScale / (double)loadImageSize.Width));//正負不影響center值(但規定要正值)
                        double height = Math.Abs(((double)info.boundingbox.Height * RealScale / (double)loadImageSize.Height));
                        string str = $"{info.Index.ToString()} {x_center.ToString("0.0000")} {y_center.ToString("0.0000")} {width.ToString("0.0000")} {height.ToString("0.0000")}";

                        txt.WriteLine(str);
                    }
                    else//代表沒有定義index過
                    {
                        //有問題!?
                    }

                }
                txt.Flush();
                txt.Close();
                grid_save_notice.Background = new SolidColorBrush(Color.FromRgb(66, 204, 45));//saved
                //CvInvoke.Rectangle(mat_boundBox, new Rectangle(0, 0, loadImageSize.Width, loadImageSize.Height), new MCvScalar(100, 200, 110), 10);
                //ImageUpdate(threadLoop);
            }
            else
            {
                MessageBox.Show("Please finish labeling");
            }

        }
        private void Btn_clearLabel_Click(object sender, RoutedEventArgs e)
        {
            LabelList = new List<LabellingInfo>();
            ReShowLabellingRect();
        }
    

        #region //---key---\\
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            this.KeyDown += new KeyEventHandler(OnButtonKeyDown);
        }
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            this.KeyDown -= new KeyEventHandler(OnButtonKeyDown);
        }
        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {

            if (Mode == ShowMode.file)
            {
                if (boundtype == BoundBox.set)
                {
                    string str = e.Key.ToString();
                    if (str.IndexOf("NumPad") >= 0)
                    {
                        LabelList.Last().Index = int.Parse(str.Substring(6));
                        //DrawLabellingBox();
                        boundtype = BoundBox.done;
                        ReShowLabellingRect();
                        //ImageUpdate(threadLoop);
                    }
                    else
                    {
                        //if (e.Key == Key.R)
                        //{
                        //CleanLabel();
                        //DrawLabellingBox();
                        //ImageUpdate(threadLoop);
                        //}
                        //else
                        MessageBox.Show("Please enter the label or cancle");
                    }
                }
                else if (boundtype == BoundBox.done || boundtype == BoundBox.none)//hot key
                {
                    if (e.Key == Key.S)
                        Btn_saveLabelling_Click(null, null);
                    else if (e.Key == Key.D)
                        Btn_nextImage_Click(null, null);
                    else if (e.Key == Key.E)
                        Btn_previousImage_Click(null, null);
                    else if (e.Key == Key.R)
                        Btn_reLoadImage_Click(null, null);
                    else if (e.Key == Key.C)
                        Btn_clearLabel_Click(null, null);
                }
            }
        }
        #endregion \\---key---//

        #region //---image mouse---\\
        enum BoundBox
        {
            none = 0,
            mouseDown = 1,
            mouseDrag = 2,//100, 200, 255
            set = 3, //100, 150, 255
            done = 4//100, 200, 110
        }
        Point mousePoint = new Point();
        BoundBox boundtype = BoundBox.none;
        Point mousePress = new Point();
        Point mouseRelease = new Point();
        System.Windows.Shapes.Rectangle setRect;

        public void ClearDragRect()
        {
            line_box_t.X1 = 0; line_box_t.Y1 = 0; line_box_t.X2 = 0; line_box_t.Y2 = 0;
            line_box_l.X1 = 0; line_box_l.Y1 = 0; line_box_l.X2 = 0; line_box_l.Y2 = 0;
            line_box_b.X1 = 0; line_box_b.Y1 = 0; line_box_b.X2 = 0; line_box_b.Y2 = 0;
            line_box_r.X1 = 0; line_box_r.Y1 = 0; line_box_r.X2 = 0; line_box_r.Y2 = 0;
        }
        public void ClearSetRect()
        {
            grid_image.Children.Remove(setRect);
        }
        public void ClearTempRect()
        {
            ClearDragRect();
            ClearSetRect();
            if (boundtype == BoundBox.set)//如果已經是set狀態(上一個沒有label完成)，就要重新刪掉上個未完成的框
            {
                LabelList.Remove(LabelList.Last());
            }
        }
        public void ReShowLabellingRect()
        {
            ClearAllShowedRect();
            foreach (LabellingInfo info in LabelList)
            {
                if (info.Index >= 0)
                {
                    System.Windows.Shapes.Rectangle rect;
                    rect = new System.Windows.Shapes.Rectangle();
                    rect.Stroke = new SolidColorBrush(Color.FromRgb(128, 201, 58));//128, 201, 58
                    rect.StrokeThickness = 2;
                    rect.HorizontalAlignment = HorizontalAlignment.Left;
                    rect.VerticalAlignment = VerticalAlignment.Top;
                    rect.Margin = new Thickness(info.boundingbox.X, info.boundingbox.Y, 0, 0);
                    rect.Height = info.boundingbox.Height;
                    rect.Width = info.boundingbox.Width;
                    grid_image.Children.Add(rect);

                    TextBlock tb = new TextBlock();
                    tb.Text = info.Index.ToString();
                    tb.TextWrapping = TextWrapping.Wrap;
                    tb.Height = 20;
                    tb.Width = 11;
                    tb.FontSize = 16;
                    tb.HorizontalAlignment = HorizontalAlignment.Left;
                    tb.VerticalAlignment = VerticalAlignment.Top;
                    tb.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    tb.Background = new SolidColorBrush(Color.FromRgb(128, 201, 58));
                    tb.Margin = new Thickness(info.boundingbox.X + 2, info.boundingbox.Y, 0, 0);
                    grid_image.Children.Add(tb);
                    //Console.WriteLine("draw rect");
                    //CvInvoke.Rectangle(mat_boundBox, info.boundingbox, new MCvScalar(100, 200, 110), 2);
                    //CvInvoke.PutText(mat_boundBox, $"[{info.Index.ToString()}]", info.boundingbox.Location, FontFace.HersheySimplex, 0.5, new MCvScalar(100, 200, 110), 1);
                }
                else//代表沒有定義index過
                {
                    //CvInvoke.Rectangle(mat_boundBox_tmp, info.boundingbox, new MCvScalar(100, 150, 255), 2);
                }
            }
        }
        public void ClearAllShowedRect()
        {
            //使用他應該就是什麼暫存的都不要了。
            ClearTempRect();

            while (grid_image.Children.Count > 10)//10個(index = 9)預設的東西，剩餘全刪除
                grid_image.Children.RemoveAt(10);

            boundtype = BoundBox.none;//並回歸none
        }

        //label
        private void Img_main_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mousePoint = new Point((int)e.GetPosition((IInputElement)sender).X, (int)e.GetPosition((IInputElement)sender).Y);
            mousePress = mousePoint;
            if (Mode == ShowMode.video)//避免在攝影機模式下使用labeling
                return;
            if (e.ChangedButton == MouseButton.Left)
            {
                ClearTempRect();
                boundtype = BoundBox.mouseDown;
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                //CleanLabel();
                //DrawLabellingBox();
                //ImageUpdate(threadLoop);
            }

        }
        private void Img_main_MouseMove(object sender, MouseEventArgs e)
        {
            mousePoint = new Point((int)e.GetPosition((IInputElement)sender).X, (int)e.GetPosition((IInputElement)sender).Y);

            line_x1.X1 = 0;
            line_x1.Y1 = mousePoint.Y;
            line_x1.X2 = mousePoint.X - 5;
            line_x1.Y2 = mousePoint.Y;
            line_x2.X1 = mousePoint.X + 5;
            line_x2.Y1 = mousePoint.Y;
            line_x2.X2 = 640;
            line_x2.Y2 = mousePoint.Y;

            line_y1.X1 = mousePoint.X;
            line_y1.Y1 = 0;
            line_y1.X2 = mousePoint.X;
            line_y1.Y2 = mousePoint.Y - 5;
            line_y2.X1 = mousePoint.X;
            line_y2.Y1 = mousePoint.Y + 5;
            line_y2.X2 = mousePoint.X;
            line_y2.Y2 = 480;



            if (Mode == ShowMode.file)
            {
                //mat_guideLine = new Mat(loadImageSize, DepthType.Cv8U, 3);
                //CvInvoke.Line(mat_guideLine, new Point(0, mousePoint.Y), new Point(loadImageSize.Width, mousePoint.Y), new MCvScalar(50, 50, 230));
                //CvInvoke.Line(mat_guideLine, new Point(mousePoint.X, 0), new Point(mousePoint.X, loadImageSize.Height), new MCvScalar(50, 50, 230));

                if (boundtype == BoundBox.mouseDown)
                {
                    mat_boundBox_tmp = new Mat(loadImageSize, DepthType.Cv8U, 3);

                    //Rectangle rect = new Rectangle(mousePress.X, mousePress.Y, mousePoint.X - mousePress.X, mousePoint.Y - mousePress.Y);
                    //CvInvoke.Rectangle(mat_boundBox_tmp, rect, new MCvScalar(100, 200, 255), 2);
                    //dragRect.Margin = new Thickness(mousePress.X, mousePress.Y, 0, 0);
                    //dragRect.Height = Math.Abs(mousePoint.Y - mousePress.Y);
                    //dragRect.Width = Math.Abs(mousePoint.X - mousePress.X);
                    line_box_t.X1 = mousePress.X;
                    line_box_t.Y1 = mousePress.Y;
                    line_box_t.X2 = mousePoint.X;
                    line_box_t.Y2 = mousePress.Y;

                    line_box_l.X1 = mousePress.X;
                    line_box_l.Y1 = mousePress.Y;
                    line_box_l.X2 = mousePress.X;
                    line_box_l.Y2 = mousePoint.Y;

                    line_box_b.X1 = mousePress.X;
                    line_box_b.Y1 = mousePoint.Y;
                    line_box_b.X2 = mousePoint.X;
                    line_box_b.Y2 = mousePoint.Y;

                    line_box_r.X1 = mousePoint.X;
                    line_box_r.Y1 = mousePress.Y;
                    line_box_r.X2 = mousePoint.X;
                    line_box_r.Y2 = mousePoint.Y;

                    if (mousePress.X < mousePoint.X)
                        line_box_b.X2 -= 2;
                    else if (mousePress.X > mousePoint.X)
                        line_box_b.X2 += 2;

                    if (mousePress.Y < mousePoint.Y)
                        line_box_r.Y2 -= 2;
                    else if (mousePress.Y > mousePoint.Y)
                        line_box_r.Y2 += 2;
                }
            }
            //ImageUpdate(threadLoop);
        }
        private void Img_main_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mousePoint = new Point((int)e.GetPosition((IInputElement)sender).X, (int)e.GetPosition((IInputElement)sender).Y);
            mouseRelease = mousePoint;
            if (Mode == ShowMode.file)
            {
                boundtype = BoundBox.set;

                //新增到labbling box裡面
                Rectangle rect = new Rectangle(mousePress.X, mousePress.Y, mouseRelease.X - mousePress.X, mouseRelease.Y - mousePress.Y);
                if (rect.Width < 0)
                {
                    rect.X = mouseRelease.X;
                    rect.Width = Math.Abs(rect.Width);
                }
                if (rect.Height < 0)
                {
                    rect.Y = mouseRelease.Y;
                    rect.Height = Math.Abs(rect.Height);
                }
                //加入label list (lock的話就直接給定index，按下數字鍵就給定index，重新按下滑鼠則刪除重新來過)
                LabelList.Add(new LabellingInfo(-1, rect));

                ClearDragRect();
                setRect = new System.Windows.Shapes.Rectangle();
                setRect.Stroke = new SolidColorBrush(Color.FromRgb(240, 179, 83));
                setRect.StrokeThickness = 2;
                setRect.HorizontalAlignment = HorizontalAlignment.Left;
                setRect.VerticalAlignment = VerticalAlignment.Top;
                setRect.Margin = new Thickness(rect.X, rect.Y, 0, 0);
                setRect.Height = rect.Height;
                setRect.Width = rect.Width;
                grid_image.Children.Add(setRect);

                //如果lock了，則進入done mode
                if (cb_lockIndex.IsChecked == true)//如果有lock index
                {
                    LabelList.Last().Index = int.Parse(tb_lockIndex.Text);
                    boundtype = BoundBox.done;
                    ReShowLabellingRect();
                }
                //ShowLabellingRect();

                //draw boundBox
                //DrawLabellingBox();
                //ImageUpdate(threadLoop);

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ReShowLabellingRect();
        }
        #endregion \\---image mouse---//

        public static void setToZero(ref Mat mat)
        {
            byte[] value = new byte[mat.Rows * mat.Cols * mat.ElementSize];
            Marshal.Copy(value, 0, mat.DataPointer, mat.Rows * mat.Cols * mat.ElementSize);
        }

        bool threadLoop = false;
        private void Cb_threadShow_Click(object sender, RoutedEventArgs e)
        {
            //ABAND!
            //if (cb_threadShow.IsChecked == true)
            //{
            //    if (mat_LoadImage == null)
            //    {
            //        cb_threadShow.IsChecked = false;
            //        MessageBox.Show("Load img == null");
            //        return;
            //    }

            //    threadLoop = true;
            //    Task.Run(() =>
            //    {
            //        while (threadLoop == true)
            //        {
            //            Dispatcher.Invoke((Action)(() => { ImageUpdate(false); }));

            //            Thread.Sleep(10);
            //        }
            //    });
            //}
            //else
            //{
            //    threadLoop = false;
            //}

        }


        private void Btn_openFolder_train_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@".\");
        }

        #region //trainning file\\
        private void Btn_addFloder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog path = new System.Windows.Forms.FolderBrowserDialog();
            path.ShowDialog();
            string SelectFolder = path.SelectedPath;
            if (SelectFolder == "")
                return;

            DirectoryInfo OpenDirectory = new DirectoryInfo(SelectFolder);
            OpenFiles = OpenDirectory.GetFiles("*.png"); //Getting Text files
            if (OpenFiles.Count() == 0)
            {
                MessageBox.Show("沒有png");
                return;
            }
            string addStr = SelectFolder.Substring(SelectFolder.LastIndexOf("\\") + 1);
            addStr += "/";
            ListViewDataCollection.Add(new ListViewData(addStr, SelectFolder, OpenFiles.Count()));
        }

        private void Lv_tranningList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string folderPath in files)
                {
                    DirectoryInfo OpenDirectory = new DirectoryInfo(folderPath);
                    try
                    {
                        OpenFiles = OpenDirectory.GetFiles("*.png"); //Getting Text files
                    }
                    catch
                    {
                        MessageBox.Show("Drop in root Folder! NOT files!");
                        return;
                    }

                    string addStr = folderPath.Substring(folderPath.LastIndexOf("\\") + 1);
                    addStr += "/";
                    ListViewDataCollection.Add(new ListViewData(addStr, folderPath, OpenFiles.Count()));
                }
            }
        }

        private void Btn_creatTrainFile_Click(object sender, RoutedEventArgs e)
        {
            //TODO:應該要確認圖片有沒有配合txt 沒有的話就不要加入
            StreamWriter sw = new StreamWriter("All.txt", false);

            //each row of listView
            foreach (ListViewData lv in ListViewDataCollection)
            {
                DirectoryInfo OpenDirectory = new DirectoryInfo(lv.realPath);
                OpenFiles = OpenDirectory.GetFiles("*.png"); //Getting Text files
                if (tb_preText.Text.LastIndexOf("/") != tb_preText.Text.Count() - 1)
                    tb_preText.Text += "/";
                //each row of png in folder
                foreach (FileInfo fi in OpenFiles)
                {
                    if (File.Exists(fi.FullName.Replace(".png", ".txt")) == true)
                        sw.WriteLine($"{tb_preText.Text}{lv.Folder}{fi.Name}");
                }
            }
            sw.Flush();
            sw.Close();
        }
        #endregion \\trainning file//

        #region //testing file\\
        private void Slider_trainninPercent_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Console.WriteLine(Slider_trainninPercent.Value.ToString());
        }

        private void Slider_trainninPercent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Console.WriteLine(Slider_trainninPercent.Value.ToString());
            int TrainPer = (int)((100 - Slider_trainninPercent.Value));

            if (tb_trainninPercent != null)
                tb_trainninPercent.Text = $"{TrainPer.ToString()} / {(100 - TrainPer).ToString()}";
        }

        private void Tb_trainFilePath_PreviewDragOver(object sender, DragEventArgs e)
        {//textbox Drag會被搶走，所以要拿回來
            e.Handled = true;
        }
        private void Tb_trainFilePath_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Count() > 1)
                {
                    MessageBox.Show("one file only");
                    return;
                }
                tb_trainFilePath.Text = files[0];
            }
        }

        private void Btn_creatTest_Click(object sender, RoutedEventArgs e)
        {
            string[] allString = System.IO.File.ReadAllLines(tb_trainFilePath.Text);
            double testPer = Slider_trainninPercent.Value;
            int gap = (int)(100.0 / testPer);//每隔多少取一個

            StreamWriter sw_train = new StreamWriter("train.txt", false);
            StreamWriter sw_test = new StreamWriter("test.txt", false);

            int count = 1;
            for (int L = 0; L < allString.Count(); L++)
            {
                if (count == gap)
                {
                    sw_test.WriteLine(allString[L]);
                    count = 1;
                }
                else
                {
                    sw_train.WriteLine(allString[L]);
                }
                count++;
            }

            sw_train.Flush();
            sw_train.Close();
            sw_test.Flush();
            sw_test.Close();
        }



        #endregion \\testing file//


    }
    public class LabellingInfo
    {
        public LabellingInfo(int index, Rectangle rect)
        {
            Index = index;
            boundingbox = rect;
        }
        public Rectangle boundingbox = new Rectangle();
        public int Index = -1;
    }

    public static class BitmapSourceConvert
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);
        public static BitmapSource MatToBitmap(IInputArray mat)
        {
            WriteableBitmap bitmap = null;
            if (((Mat)mat).NumberOfChannels == 4)
                bitmap = new WriteableBitmap(((Mat)mat).Width, ((Mat)mat).Height, 96.0, 96.0, System.Windows.Media.PixelFormats.Bgra32, null);
            else if (((Mat)mat).NumberOfChannels == 3)
                bitmap = new WriteableBitmap(((Mat)mat).Width, ((Mat)mat).Height, 96.0, 96.0, System.Windows.Media.PixelFormats.Bgr24, null);
            else if (((Mat)mat).NumberOfChannels == 1)
                bitmap = new WriteableBitmap(((Mat)mat).Width, ((Mat)mat).Height, 96.0, 96.0, System.Windows.Media.PixelFormats.Gray8, null);

            bitmap.Lock();
            unsafe
            {
                var region = new Int32Rect(0, 0, ((Mat)mat).Width, ((Mat)mat).Height);
                int ch = ((Mat)mat).NumberOfChannels;
                int stride = (((Mat)mat).Width * ch);
                int bitPerPixCh = 8;
                bitmap.WritePixels(region, ((Mat)mat).DataPointer, (stride * ((Mat)mat).Height), stride);
                bitmap.AddDirtyRect(region);
            }
            bitmap.Unlock();

            return bitmap;
        }
        //public static BitmapSource ToBitmapSource(IImage image)
        //{

        //    using (System.Drawing.Bitmap source = image.Bitmap)
        //    {
        //        IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

        //        BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
        //            ptr,
        //            IntPtr.Zero,
        //            Int32Rect.Empty,
        //            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

        //        DeleteObject(ptr); //release the HBitmap
        //        return bs;
        //    }
        //}
    }

    public class ListViewData : INotifyPropertyChanged
    {
        string pre = "";
        string folder;
        public string realPath;
        int count;
        public ListViewData(string Folder, string RealPath, int Count, SolidColorBrush C1 = null, SolidColorBrush C2 = null, SolidColorBrush C3 = null)
        {
            realPath = RealPath;
            folder = Folder;
            count = Count;
            if (C1 == null)
                Color1 = new SolidColorBrush(Colors.Black);
        }

        public string Pre
        {
            set
            {
                pre = value;
                NotifyPropertyChanged("Pre");
            }
            get { return pre; }
        }
        public string Folder
        {
            set
            {
                folder = value;
                NotifyPropertyChanged("Folder");
            }
            get { return folder; }
        }
        public int Count
        {
            set
            {
                count = value;
                NotifyPropertyChanged("Count");
            }
            get { return count; }
        }
        public SolidColorBrush Color_back
        {
            set
            {
                color_back = value;
                NotifyPropertyChanged("Color_back");
            }
            get { return color_back; }
        }


        SolidColorBrush color_back = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush Color1 { get; set; } = new SolidColorBrush(Colors.Black);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
        }
    }

    public static class Ex
    {
        public static double ToDouble(this string str)
        {
            return double.Parse(str);
        }
        public static int ToInt(this string str)
        {
            return int.Parse(str);
        }
        public static void AddLayer(this IInputOutputArray baseImg, Mat upLayer)
        {
            CvInvoke.Imwrite($"baseImg.png", baseImg);
            CvInvoke.Imwrite($"uplayer.png", upLayer);
            Mat mask = new Mat(((Mat)upLayer).Size, DepthType.Cv8U, 1);
            CvInvoke.CvtColor(upLayer, mask, ColorConversion.Bgr2Gray);
            CvInvoke.Imwrite($"mask.png", mask);
            CvInvoke.Threshold(mask, mask, 0, 255, ThresholdType.Binary);
            CvInvoke.Imwrite($"thres.png", mask);
            ((Mat)upLayer).CopyTo(baseImg, mask);
            CvInvoke.Imwrite($"out.png", baseImg);
        }
        public static void setToZero(ref Mat mat)
        {
            byte[] value = new byte[mat.Rows * mat.Cols * mat.ElementSize];
            Marshal.Copy(value, 0, mat.DataPointer, mat.Rows * mat.Cols * mat.ElementSize);
        }
    }

}
