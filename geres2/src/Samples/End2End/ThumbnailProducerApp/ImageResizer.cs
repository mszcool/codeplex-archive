//
// Copyright (c) Microsoft.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//           http://www.apache.org/licenses/LICENSE-2.0 
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;


namespace ThumbnailProducerApp
{
    public class ImageResizer
    {
        public bool OverwriteFiles { get; set; }
        public bool RemainAspectRatio { get; set; }
        public int TargetHeight { get; set; }
        public int TargetWidth { get; set; }

        public ImageResizer(bool remainAspectRatio, bool overwriteFiles, int targetHeight, int targetWidth)
        {
            this.OverwriteFiles = overwriteFiles;
            this.RemainAspectRatio = remainAspectRatio;
            this.TargetHeight = targetHeight;
            this.TargetWidth = targetWidth;
        }

        public void ResizeImage(string inputFile, string outputFile)
        {
            int finalTargetWidth = TargetWidth;
            int finalTargetHeight = TargetHeight;

            //
            // First of all validate input-parameters according to settings and perform necessary pre-processing steps
            //
            CheckParameters(inputFile, outputFile);
            PreProcessing(outputFile);

            //
            // Next load the original image and get the original size
            //
            var originalImage = System.Drawing.Image.FromFile(inputFile);
            var originalWidth = originalImage.Width;
            var originalHeight = originalImage.Height;

            //
            // If the aspect ratio should be kept, determine, whether the height or the width is specified. Prioritize by height.
            //
            if (RemainAspectRatio)
            {
                if (TargetHeight > 0)
                {
                    // Calculate the aspect-ratio based on the height
                    var percentage = ((float)TargetHeight * 100 / (float)originalHeight);
                    finalTargetHeight = TargetHeight;
                    finalTargetWidth = (int)(originalWidth * percentage / 100);
                }
                else if (TargetWidth > 0)
                {
                    // Calculate the aspect-ratio based on the width
                    var percentage = ((float)TargetWidth * 100 / (float)originalWidth);
                    finalTargetWidth = TargetWidth;
                    finalTargetHeight = (int)(originalHeight * percentage / 100);
                }

                // If one or the other parameter resulted in "0", then the percentage was too small
                if (finalTargetHeight <= 0 || finalTargetWidth <= 0)
                    throw new ApplicationException("The aspect ratio with resizing the image cannot be met. Please try other target sizes (either height or width) or turn off remaining aspect ratio!");
            }

            // 
            // Finally resize the image with the resulting target height and width and save it under the target-name
            //
            var resultingImage = originalImage.GetThumbnailImage
                                        (
                                            finalTargetWidth, 
                                            finalTargetHeight,
                                            new Image.GetThumbnailImageAbort(() => { return false; }), 
                                            IntPtr.Zero
                                        );
            resultingImage.Save(outputFile, ImageFormat.Jpeg);
        }


        private void CheckParameters(string inputFile, string outputFile)
        {
            // If aspect ratio should be kept, target width or height must be > 0, otherwise both must be > 0.
            if (RemainAspectRatio)
            {
                if (TargetWidth <= 0 && TargetHeight <= 0)
                    throw new ApplicationException("If you want to remain aspect ratio, either targetdWidth or targetdHeight must be > 0 to calculate the target size!");
            }
            else if((TargetWidth <= 0) || (TargetHeight <= 0))
            {
                throw new ApplicationException("If you don't want to remain the aspect ratio, both, targetWidth and targetHeight must be > 0 to specify the target size!");
            }

            // Check if the input-file does exist
            if (!System.IO.File.Exists(inputFile))
            {
                throw new ApplicationException(string.Format("The input file on the path '{0}' does not exist!", inputFile));
            }

            // Check if the output-file does exist
            if (System.IO.File.Exists(outputFile))
            {
                if (!OverwriteFiles)
                    throw new ApplicationException(string.Format("The target file '{0}' does exist and converter is configured to not overwrite target files!", outputFile));
            }
        }

        private static void PreProcessing(string outputFile)
        {
            // Try deleting the target file if it does exist
            try
            {
                if (System.IO.File.Exists(outputFile))
                    System.IO.File.Delete(outputFile);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Unable to delete output file '{0}'", outputFile), ex);
            }
        }
    }
}
