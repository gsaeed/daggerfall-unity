// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using UnityEngine;

namespace DaggerfallWorkshop.Game.UserInterface
{
    /// <summary>
    /// Renders an image in place of a text label in book reader UI.
    /// </summary>
    public class ImageLabel : TextLabel
    {
        Texture2D image;
        float imageWidth;
        float imageHeight;
        float scaleFactor;
        public int scale;
        public int width;
        public int height;
        public bool custom = false;

        public Texture2D Image
        {
            get { return image; }
            set { image = value; RefreshLayout(); }
        }

        public override void Draw()
        {
            if (image == null || image.width == 0 || image.height == 0)
                return;

            // Image position is always centred to page
            RefreshLayout();
            if (custom)
            {
                Rect totalRect = Rectangle;
                // Center the image within the totalRect
                float xOffset = (totalRect.width - imageWidth) / 2;
                Rect rect = new Rect(totalRect.x + xOffset, totalRect.y, imageWidth, imageHeight);
                Size = new Vector2(imageWidth, imageHeight);
                DaggerfallUI.DrawTexture(rect, image, ScaleMode.ScaleToFit);

            }
            else
            {
                Rect totalRect = Rectangle;
                Rect rect = new Rect(totalRect.x + imageWidth / 2, totalRect.y, imageWidth, imageHeight);
                Size = new Vector2(imageWidth, imageHeight);
                DaggerfallUI.DrawTexture(rect, image, ScaleMode.StretchToFill);
            }

            
        }

        public override void RefreshLayout()
        {
            if (image == null || image.width == 0 || image.height == 0)
                return;

            base.RefreshLayout();

            if (custom)
            {
                if (scale > 0)
                {
                    width = Mathf.RoundToInt(image.width * scale / 100f);
                    height = Mathf.RoundToInt(image.height * scale / 100f);
                }

                //imageWidth = width * LocalScale.x / 2f;
                //imageHeight = height * LocalScale.y / 2f;
                //imageWidth = (float)width * LocalScale.x;
                //imageHeight = (float)height * LocalScale.y;
                imageWidth = (float)width;
                imageHeight = (float)height;
                scaleFactor = 1f;
                Size = new Vector2(imageWidth, imageHeight);
            }
            else
            {
                // Image size is always half width of page area
                imageWidth = (float)MaxWidth * LocalScale.x / 2f;
                scaleFactor = (float)MaxWidth / (float)image.width;
                imageHeight = (float)image.height * scaleFactor * LocalScale.y / 2f;
                Size = new Vector2(imageWidth, imageHeight);
            }
        }
    }
}