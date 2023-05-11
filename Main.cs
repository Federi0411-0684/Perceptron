using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SharpAI
{
    public partial class Main : Form
    {
        private bool selected = false;
        private string trainingMainPath = string.Empty;
        private string[] trainingPaths = new string[0];
        private string[] names = new string[0];
        private readonly string masksPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ICModelMasks";
        ICModel model = new ICModel();

        public Main()
        {
            InitializeComponent();

            Debug.WriteLine(this.masksPath);

            //initialise model
            this.model = new ICModel(this.masksPath);

            //set background color
            this.BackColor = Color.FromArgb(255, 44, 57, 82);
            this.menuPanel.BackColor = Color.FromArgb(50, 34, 47, 62);

            //set buttons
            Color secondary = Color.FromArgb(255, 34, 47, 62);
            this.trainBtn.BackColor = secondary;
            this.startBtn.BackColor = secondary;
            this.testBtn.BackColor = secondary;

            //resize and center picturebox
            this.pictureBox.Size = new Size(this.pictureBox.Size.Height, this.pictureBox.Size.Height);
            this.pictureBox.Location = new Point((this.Size.Width - this.pictureBox.Size.Width) / 2, 0);
            this.pictureBox.AllowDrop = true;
            this.onSizeChanged();
        }

        private void PaintForm(object sender, PaintEventArgs e)
        {
            //paint background
            Graphics g = e.Graphics;

            //get rectangle of form
            Rectangle rect = ClientRectangle;

            //make darker gradient color 
            int darkness = 55;
            int R = (((this.BackColor.R - darkness) > 0) ? (this.BackColor.R - darkness) : 0);
            int G = (((this.BackColor.G - darkness) > 0) ? (this.BackColor.G - darkness) : 0);
            int B = (((this.BackColor.B - darkness) > 0) ? (this.BackColor.B - darkness) : 0);
            Color darker = Color.FromArgb(255, R, G, B);

            //draw gradient
            LinearGradientBrush brush = new LinearGradientBrush(new PointF(0, 0), new PointF(0, rect.Height), this.BackColor, darker);
            g.FillRectangle(brush, rect);
        }

        private void Form_Paint(object sender, PaintEventArgs e)
        {
            //draw the background gradient
            this.PaintForm(sender, e);
        }

        private void Form_SizeChanged(object sender, EventArgs e)
        {
            this.onSizeChanged();
        }

        private void onSizeChanged()
        {
            //draw again when the form changes size
            this.Invalidate();

            //resize and center picturebox
            this.pictureBox.Size = new Size(this.pictureBox.Size.Height, this.pictureBox.Size.Height);
            this.pictureBox.Location = new Point((this.Size.Width - this.pictureBox.Size.Width) / 2, 0);
        }

        private void pictureBox_DragDrop(object sender, DragEventArgs e)
        {
            //get dropped file data
            object data = e.Data.GetData(DataFormats.FileDrop);

            //if data is valid
            if (data != null)
            {
                string[] names = data as string[];

                if (names.Length > 0)
                {
                    this.loadImage(names[0]);
                }
            }
        }

        private void pictureBox_DragEnter(object sender, DragEventArgs e)
        {
            //show little copy-drop icon when dragging a picture over the box
            e.Effect = DragDropEffects.Copy;
        }

        private Image formatImage(Image img)
        {
            //initialise final bitmap
            Bitmap res = new Bitmap(1024, 1024);

            //setup initial bitmap from image
            Bitmap bmp;
            if (img.Width > img.Height)
            {
                //scale bmp to have the smallest side, the height, equal to 1024
                float scaleFactor = 1024.0F / (float)img.Height;
                bmp = new Bitmap((Bitmap)img, new Size((int)(scaleFactor * (float)img.Width), 1024));

                //crop bmp on res
                using (Graphics graphics = Graphics.FromImage(res))
                {
                    graphics.DrawImage(bmp, new Point(-((bmp.Width - 1024) / 2), 0));
                }
            }
            else
            {
                //scale bmp to have the smallest side, the width, equal to 1024
                float scaleFactor = 1024.0F / (float)img.Width;
                bmp = new Bitmap((Bitmap)img, new Size(1024, (int)(scaleFactor * (float)img.Height)));

                //crop bmp on res
                using (Graphics graphics = Graphics.FromImage(res))
                {
                    graphics.DrawImage(bmp, new Point(0, -((bmp.Height - 1024) / 2)));
                }
            }

            return (Image)res;
        }

        /*Bitmap res2 = new Bitmap(1024, 1024, PixelFormat.Format16bppGrayScale);

            BitmapData data = res.LockBits(new Rectangle(0, 0, 1024, 1024), ImageLockMode.ReadWrite, res.PixelFormat);

            BitmapData data2 = res2.LockBits(new Rectangle(0, 0, 1024, 1024), ImageLockMode.ReadWrite, res2.PixelFormat);

            //put the data from res in the array
            int size = 4 * 1024 * 1024;
            int size2 = 2 * 1024 * 1024;
            byte[] bytes = new byte[size];
            byte[] bytes2 = new byte[size2];
            IntPtr p = data.Scan0;
            IntPtr p2 = data2.Scan0;
            Marshal.Copy(p, bytes, 0, size);
            Marshal.Copy(p2, bytes2, 0, size2);

            //print
            for (int i = 0; i < size; i++)
            {
                bytes[i] = 254;
                int r = (i % 3) / 1024 / 4;
                int c = (i % 3) - (r * 1024 * 4);
                if (r == c)
                {
                    bytes[i] = 0;
                }
            }

            Marshal.Copy(bytes, 0, p, size);

            res.UnlockBits(data);
            res2.UnlockBits(data2);
            */

        private void pictureBox_Click(object sender, EventArgs e)
        {
            //open dialog
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image Files(*.png; *.jpg; *.jpeg; *.bmp)|*.png; *.jpg; *.jpeg; *.bmp";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                //get selected image and load it
                this.loadImage(dialog.FileName);
            }
        }

        private void loadImage(string path)
        {
            //make sure that the picturebox is in the right mode
            if (this.pictureBox.SizeMode != PictureBoxSizeMode.Zoom)
            {
                this.pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
            //format and load the picture
            this.pictureBox.Image = this.formatImage(Image.FromFile(path));

            //set selected to true so that the image can be classified
            this.selected = true;
        }

        private void trainBtn_Click(object sender, EventArgs e)
        {
            this.startTraining();
        }

        private bool startTraining()
        {
            //get training folder path
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                //open training folder selection
                DialogResult result = dialog.ShowDialog();

                //if the path is valid
                if ((result == DialogResult.OK) && (!string.IsNullOrWhiteSpace(dialog.SelectedPath)))
                {
                    //save it
                    this.trainingMainPath = dialog.SelectedPath;

                    //train model
                    if (this.model.train(this.trainingMainPath))
                    {
                        //if training is successful, add training path to the settings
                        Properties.Settings.Default.Trained = this.trainingMainPath;
                        Properties.Settings.Default.Save();
                        return true;
                    }
                    else
                    {
                        //if the training did not go well
                        DialogResult msg = MessageBox.Show("The model was not able to be trained with the selected dataset", "Training failed", MessageBoxButtons.OK);
                    }
                }
            }
            return false;
        }

        private void startBtn_Click(object sender, EventArgs e)
        {
            //if not trained, force training
            if (!this.model.trained(Properties.Settings.Default.Trained))
            {
                DialogResult msg = MessageBox.Show("Do you want to train the model?", "The model is not trained", MessageBoxButtons.YesNo);
                if (msg == DialogResult.Yes)
                {
                    //train and then classify if training goes well
                    if (this.startTraining())
                    {
                        this.startClassification();
                    }
                    else
                    {
                        DialogResult msg2 = MessageBox.Show("Training failed", "Error", MessageBoxButtons.OK);
                    }
                }
            }
            else
            {
                //if the model is already trained, 
                this.startClassification();
            }
        }

        private void startClassification()
        {
            //if the user has not selected an image yet
            if ((!this.selected) || (this.pictureBox.Image == null))
            {
                DialogResult msg = MessageBox.Show("Open an image to be classified", "Classify", MessageBoxButtons.OK);
            }
            else
            {
                //then classify the image
                this.model.classify((Bitmap)this.pictureBox.Image);
            }
        }

        private void testBtn_Click(object sender, EventArgs e)
        {
            //test model
        }
    }
}
