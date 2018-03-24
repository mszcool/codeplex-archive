// <copyright file="Program.cs" company="Personal">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <author>Mario Szpuszta</author>
// <date>2013-8-7, 10:44</date>
// <summary>This is a sample and demo - use it at your full own risk!</summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThumbnailProducerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string sourceImage, targetImage;
            SupportedImageSizes targetImageSize;
            int targetHeight, targetWidth;

            CheckParameters(args, out sourceImage, out targetImage, out targetImageSize, out targetWidth, out targetHeight);

            switch (targetImageSize)
            {
                case SupportedImageSizes.Large:
                    targetHeight = targetWidth = 32;
                    break;
                case SupportedImageSizes.Medium:
                    targetHeight = targetWidth = 16;
                    break;
                case SupportedImageSizes.Small:
                    targetHeight = targetWidth = 8;
                    break;
                default:
                    // Target width and height has been parsed by CheckParameters
                    break;
            }

            try
            {
                Console.WriteLine("Starting image conversion...");
                var resizer = new ImageResizer
                                    (
                                        targetImageSize != SupportedImageSizes.Custom, 
                                        true, 
                                        targetHeight, 
                                        targetWidth
                                    );
                resizer.ResizeImage(sourceImage, targetImage);
                Console.WriteLine("Converted image successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Exception occured: {0}", ex.Message));
                System.Environment.Exit(20);
            }

            System.Environment.Exit(0);
        }

        private static void CheckParameters(string[] args, out string sourceImage, out string targetImage, out SupportedImageSizes targetImageSize, out int targetWidth, out int targetHeight)
        {
            sourceImage = string.Empty;
            targetImage = string.Empty;
            targetHeight = 0;
            targetWidth = 0;
            targetImageSize = SupportedImageSizes.Medium;

            //
            // Expects three arguments - source image, target image, and image-size
            //
            Console.WriteLine("Checking parameters...");
            if (args.Length < 3)
            {
                Console.WriteLine("Please call with 'thumbnailproducerapp sourceImage targetImage targetSize [optional: height] [optional: width]");
                System.Environment.Exit(10);
                return;
            }
            else
            {
                sourceImage = args[0];
                targetImage = args[1];
                if (!Enum.TryParse(args[2], out targetImageSize))
                {
                    Console.WriteLine("Please use one of the following values for targetSize: Large, Medium, Small");
                    System.Environment.Exit(10);
                    return;
                }
                else
                {
                    if (targetImageSize == SupportedImageSizes.Custom)
                    {
                        if (args.Length != 5)
                        {
                            Console.WriteLine("Please call with 'thumbnailproducerapp sourceImage targetImage Custom height width");
                        }
                        else
                        {
                            if (!int.TryParse(args[3], out targetHeight))
                            {
                                Console.WriteLine("Cannot parse targetHeight specified. Please specify a full number!");
                                System.Environment.Exit(10);
                                return;
                            }

                            if (!int.TryParse(args[4], out targetWidth))
                            {
                                Console.WriteLine("Cannot parse targetWidth specified. Please specify a full number!");
                                System.Environment.Exit(10);
                                return;
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Parameters checked successfully!");
        }
    }
}
