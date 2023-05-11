using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpAI
{
    internal class ICModel
    {
        private byte[,] masks;
        private string masksPath;
        private string mainTrainingPath;
        private string[] trainingDirectories;
        private string[] trainingNames;

        private float threshold = 0.5F;

        public ICModel()
        {
            this.masks = null;
            this.masksPath = null;
            this.mainTrainingPath = null;
            this.trainingDirectories = null;
            this.trainingNames = null;
        }

        public ICModel(string masks_path = null, string main_tr_path = null, string[] training_directories = null)
        {
            this.masks = null;
            this.masksPath = masks_path;
            this.mainTrainingPath = main_tr_path;
            this.trainingDirectories = ((training_directories == null) ? this.getDirectories() : training_directories);
            this.trainingNames = ((main_tr_path == null) ? null : this.getNames());

            //get masks if possible
            if ((this.trainingNames != null) && (this.trainingNames.Length > 0) && (masks_path != null))
            {
                this.getAllMasks();
            }
        }

        public bool train(string main_path = null, string[] training_directories = null, string[] names = null)
        {
            //get paths
            this.mainTrainingPath = main_path;
            this.trainingDirectories = ((training_directories == null) ? this.getDirectories() : training_directories);
            this.trainingNames = ((names == null) ? this.getNames() : names);

            //train model
            return this.train();
        }

        public bool train()
        {
            //if the masks are not defined, make them
            if (this.masks == null)
            {
                //if the mask path is also not valid, make an empty one
                if ((this.masksPath == string.Empty) || (this.masksPath == null))
                {
                    this.masks = new byte[this.trainingNames.Length, 4 * 1024 * 1024];
                }
                else //if the path is valid, load the mask from the path
                {
                    //create directory if it does not exist
                    if (Directory.Exists(this.masksPath))
                    {
                        Directory.CreateDirectory(this.masksPath);
                    }

                    try
                    {
                        //get masks
                        this.getAllMasks();
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            //do the actual training...

            //do the training for each category
            for (int i = 0; i < this.trainingNames.Length; i++)
            {
                //get list of all pictures of the current category
                string[] pics = Directory.GetFiles(this.trainingNames[i], "*.bmp", SearchOption.TopDirectoryOnly);

                //do the training with all the pics
                for (int j = 0; j < pics.Length; j++)
                {
                    byte[] pic = ICModel.imageToMask(Image.FromFile(pics[j]));
                    byte trainingK = (byte)((this.getOutput(this.getMaskFromIndex(i), pic) < this.threshold) ? 1 : -1);

                    //modify mask based on output
                    for (int k = 0; k < (4 * 1024 * 1024); k++)
                    {
                        this.masks[i, k] += (byte)(trainingK * pic[k]);
                    }
                }
            }

            //save the masks
            
            return true;
        }

        private void getAllMasks()
        {
            //get mask paths
            string[] maskPaths = new string[this.trainingNames.Length];
            for (int i = 0; i < maskPaths.Length; i++)
            {
                maskPaths[i] = this.getPathToMask(this.trainingNames[i]);
            }

            //get images from the maskspath, transform them in a 1024*1024 bitmap and then convert it into byte array
            for (int i = 0; i < maskPaths.Length; i++)
            {
                //get byte array from image from path
                byte[] tmpMask = ICModel.imageToMask(new Bitmap(Image.FromFile(maskPaths[i]), new Size(1024, 1024)));

                //add the array to the matrix
                for (int j = 0; j < tmpMask.Length; j++)
                {
                    this.masks[i, j] = tmpMask[j];
                }
            }
        }

        private byte[] getMaskFromIndex(int index)
        {
            byte[] res = new byte[4 * 1024 * 1024];

            //get indexth line of matrix
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = this.masks[index, i];
            }

            return res;
        }

        public float[] classify(string path)
        {
            return this.classify(Image.FromFile(path));
        }

        public float[] classify(Image img)
        {
            return this.classify(new Bitmap(img));
        }

        public float[] classify(Bitmap bmp)
        {
            //declare classification array
            float[] res = new float[this.trainingNames.Length];

            //scale, make greyscale and format the image properly
            bmp = ICModel.formatAndScale(bmp);

            //start classification
            for (int i = 0; i < this.trainingNames.Length; i++)
            {
                res[i] = getOutput((Bitmap)Image.FromFile(this.getPathToMask(this.trainingNames[i])), bmp);
            }

            return res;
        }

        private float getOutput(Bitmap mask, Bitmap pic)
        {
            return this.getOutput(ICModel.imageToMask(mask), ICModel.imageToMask(pic));
        }

        private float getOutput(byte[] mask, byte[] pic)
        {
            int sum = 0;
            //find vector dot product
            for (int i = 0; i < (4 * 1024 * 1024); i++)
            {
                sum += (int)(mask[i] * pic[i]);
            }

            //divide it by the maximum possible value to find the percentage
            return ((float)sum / (4F * 1024F * 1024F * 256F * 256F));
        }

        public string[] getDirectories()
        {
            if (this.mainTrainingPath != null)
            {
                //get subdirectories of parameter path
                string[] dirs = Directory.GetDirectories(this.mainTrainingPath, "*", SearchOption.TopDirectoryOnly);
                List<string> res = new List<string>();

                //only add the directories that have something in them
                foreach (string dir in dirs)
                {
                    if (Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length > 0)
                    {
                        res.Add(dir);
                    }
                }

                return res.ToArray();
            }
            else
            {
                return null;
            }
        }

        public string[] getNames()
        {
            if (this.trainingDirectories != null)
            {
                List<string> res = new List<string>();

                //add to the list only the portion of the string after the last '\' or '/'
                foreach (string path in this.trainingDirectories)
                {
                    string[] words = path.Split(new char[] { '\\', '/' });
                    res.Add(words[words.Length - 1]);
                }

                return res.ToArray();
            }
            else
            {
                return null;
            }
        }

        public bool trained(string path)
        {
            return (this.mainTrainingPath == path);
        }

        public byte[,] Masks
        {
            get { return this.masks; }
            set { this.masks = value; }
        }

        public string MasksPath
        {
            get { return this.masksPath; }
            set { this.masksPath = value; }
        }

        public string MainTrainingPath
        {
            set { this.mainTrainingPath = value; }
            get { return this.mainTrainingPath; }
        }

        public string[] TrainingDirectories
        {
            get { return this.trainingDirectories; }
            set { this.trainingDirectories = value; }
        }

        public string[] TrainingNames
        {
            get { return this.trainingNames; }
            set { this.trainingNames = value; }
        }

        private string getPathToMask(string name)
        {
            return (this.masksPath + "\\" + name + ".bmp");
        }

        private static Bitmap formatAndScale(Bitmap bmp0)
        {
            //make it greyscale
            bmp0 = ICModel.makeGrayScale(bmp0);

            //initialise final bitmap
            Bitmap bmp = new Bitmap(1024, 1024);

            //setup initial bitmap from image
            Bitmap bmp2;
            if (bmp0.Width > bmp0.Height)
            {
                //scale bmp to have the smallest side, the height, equal to 1024
                float scaleFactor = 1024.0F / (float)bmp0.Height;
                bmp2 = new Bitmap(bmp0, new Size((int)(scaleFactor * (float)bmp0.Width), 1024));

                //crop bmp on res
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    graphics.DrawImage(bmp2, new Point(-((bmp2.Width - 1024) / 2), 0));
                }
            }
            else
            {
                //scale bmp to have the smallest side, the width, equal to 1024
                float scaleFactor = 1024.0F / (float)bmp0.Width;
                bmp2 = new Bitmap(bmp0, new Size(1024, (int)(scaleFactor * (float)bmp0.Height)));

                //crop bmp on res
                using (Graphics graphics = Graphics.FromImage(bmp))
                {
                    graphics.DrawImage(bmp2, new Point(0, -((bmp2.Height - 1024) / 2)));
                }
            }

            return bmp;
        }

        private static byte[] imageToMask(string path)
        {
            return ICModel.imageToMask(Image.FromFile(path));
        }

        private static byte[] imageToMask(Image img)
        {
            return ICModel.imageToMask(new Bitmap(img));
        }

        private static byte[] imageToMask(Bitmap bmp)
        {
            //make sure that the image is in the right format 
            bmp = new Bitmap(bmp, 1024, 1024);

            //lock bits
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            
            //get bits from bitmap to the array
            byte[] res = new byte[4 * 1024 * 1024];
            Marshal.Copy(data.Scan0, res, 0, 4 * 1024 * 1024);

            //unlck bits
            bmp.UnlockBits(data);

            return res;
        }

        public static Bitmap makeGrayScale(Image img)
        {
            return ICModel.makeGrayScale(new Bitmap(img));
        }

        public static Bitmap makeGrayScale(Bitmap bmp)
        {
            //create a blank bitmap the same size as original
            Bitmap res = new Bitmap(bmp.Width, bmp.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(res);

            //create image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the grayscale matrix attribute
            attributes.SetColorMatrix
            (
                new ColorMatrix
                (
                    new float[][]
                    {
                        new float[] { 0.3F, 0.3F, 0.3F, 0.0F, 0.0F },
                        new float[] { 0.59F, 0.59F, 0.59F, 0.0F, 0.0F },
                        new float[] { 0.11F, 0.11F, 0.11F, 0.0F, 0.0F },
                        new float[] { 0.0F, 0.0F, 0.0F, 1.0F, 0.0F },
                        new float[] { 0.0F, 0.0F, 0.0F, 0.0F, 1.0F }
                    }
                )
            );

            //draw the original image on the new image using the grayscale color matrix
            g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return res;
        }
    }
}
