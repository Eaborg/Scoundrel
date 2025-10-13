using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static System.Math;

namespace Scoundrel.Code
{
    enum FitType
    {
        Fixed, // the object's size is static, not changed by layout()
        Fit, // the object's size is changed by layout() to fit around it's children
    }
    enum Alignment
    {
        Negative, // negative direction of either axis, so left or up
        Centred, // neutral position. Pretty self explanatory
        Positive, // positive direction of either axis, so right or down
    }
    enum Axis
    {
        X,
        Y
    }

    internal class UINode()
    {
        public List<UINode> children = new List<UINode>(); // the child ui nodes
        public Rectangle body = Rectangle.Empty; // the size and position
        public int childGap = 0; // the gap between all of the children
        public int innerMargin = 0; // the gap between the border and the children
        public FitType widthFit = FitType.Fixed; // whether to fit the width to the children or not
        public FitType heightFit = FitType.Fixed; // whether to fit the height to the children or not
        public Alignment xAlignment = Alignment.Negative; // how it should position its children left-right
        public Alignment yAlignment = Alignment.Negative;   // how it should position its children up-down
        public Color color = Color.Transparent; // the relevant colour of the UI object
        public Axis LayoutDirection = Axis.Y;

        // wrappers for getting/setting the size and position of objects on arbitrary axes
        public FitType getFitType(Axis axis) => 
            axis == Axis.X ? widthFit : heightFit;
        public int getSize(Axis axis) => 
            axis==Axis.X? body.Width : body.Height;
        public void setSize(Axis axis, int size) 
        { if (axis == Axis.X) body.Width = size; else body.Height = size; }
        public int getPos(Axis axis) => 
            axis == Axis.X ? body.X : body.Y;
        public void setPos(Axis axis, int pos) 
        { if (axis == Axis.X) body.X = pos; else body.Y = pos; }
        // aggregate functions for child sizing
        public int getMaxChildSize(Axis axis) =>
            Enumerable.Aggregate(children, Int32.MinValue, (int total, UINode node)=>Max(total, node.UpdateSize(axis)) );
        public int getTotalChildSize(Axis axis) =>
            Enumerable.Aggregate(children, 0, (int total, UINode node) => (total + node.UpdateSize(axis)) );



        // constructor for copying a template instance of a UINode
        public UINode(UINode template) : this()
        {
            //this.children = template.children;
            body = template.body;
            childGap = template.childGap;
            innerMargin = template.innerMargin;
            widthFit = template.widthFit;
            heightFit = template.heightFit;
            xAlignment = template.xAlignment;
            yAlignment = template.yAlignment;
            color = template.color;
            LayoutDirection = template.LayoutDirection;
        }

        // draw method. This changes depending on the kind of object it is.
        public virtual void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            // rectangle drawing method
            Texture2D _texture;
            _texture = new Texture2D(graphicsDevice, 1, 1);
            _texture.SetData([color]);
            spriteBatch.Draw(_texture, body, Color.White);

            // draw it's children
            foreach (UINode child in children)
                child.Draw(spriteBatch, graphicsDevice);
        }
        // calculates and sets this node's size and position, as well as
        // the size and position of all its children in the process
        public void layout()
        {
            //UpdateWidth();
            //UpdateHeight();
            UpdateSize(Axis.X);
            UpdateSize(Axis.Y);
            updateXPositions(body.X);
            updateYPositions(body.Y);
        }
        // method for adding children during the initialization of the UI node
        public UINode Add(params UINode[] elements)
        {
            foreach (UINode element in elements)
                children.Add(element);
            
            return this;
        }
        public virtual int UpdateSize(Axis axis)
        {
            // if the size is fixed then just update the children
            if (getFitType(axis) == FitType.Fixed)
            {
                // update each child's width
                foreach (UINode child in children)
                    child.UpdateSize(axis);

                return getSize(axis);
            }
            else// (the fit type is fit)
            {
                // if the axis and layout direction are different, then the size is the max, otherwise it's the total
                int size = (LayoutDirection != axis) ?
                    getMaxChildSize(axis) + 2*innerMargin :
                    getTotalChildSize(axis) + 2*innerMargin + (children.Count - 1)*childGap ;
                setSize(axis, size);
                return size;
            }
        }
        public virtual int UpdateWidth()
        {
            // if the size is fixed then just update the children
            if (widthFit == FitType.Fixed)
            {
                // update each child's width
                foreach (UINode child in children)
                    child.UpdateWidth();

                return body.Width;
            }
            // else the fittype is fit
            int maxChildWidth = 0;
            foreach (UINode child in children)
                maxChildWidth = Max(maxChildWidth, child.UpdateWidth());

            body.Width = maxChildWidth + 2 * innerMargin;
            return body.Width;
        }
        public virtual int UpdateHeight()
        {
            if (heightFit == FitType.Fixed)
            {
                // update each child's height
                foreach (UINode child in children)
                    child.UpdateHeight();

                return body.Height;
            }
            // else the fittype is fit
            int total = 0;
            foreach (UINode child in children) total += child.UpdateHeight();
            body.Height = total + 2*innerMargin + (children.Count-1)*childGap;
            return body.Height;
        }
        public void updateXPositions(int originX)
        {
            // set this object's position
            body.X = originX;

            // find where to position all of it's children.
            if (xAlignment == Alignment.Negative)
            {
                // if this object is aligned to the left
                // then the children only need to take into account the inner margin
                foreach (UINode child in children)
                    child.updateXPositions(originX + innerMargin);
            }
            else//(fittype is fit and alignment isn't negative)
            {
                if (xAlignment == Alignment.Centred)
                {
                    // if children are aligned to the middle then
                    // half of the remaining width needs to be added
                    foreach (UINode child in children)
                        child.updateXPositions(originX + (body.Width - child.body.Width)/2);
                }
                else// (alignment is positive)
                {
                    // if the children are aligned to the right then
                    // add all of the remaining width minus the inner margin
                    foreach (UINode child in children)
                        child.updateXPositions(originX + body.Width - child.body.Width - innerMargin);
                }
            }
        }
        public void updateYPositions(int originY)
        {
            // set this object's position
            body.Y = originY;

            // handle the Y positioning of it's children
            if (heightFit == FitType.Fit || yAlignment == Alignment.Negative)
            {
                // if this object fits to it's children or is aligned to the top
                // then the children only need to take into account the inner margin
                originY += innerMargin;
            }
            else//(fittype is fit and alignment isn't negative)
            {
                // find the total height of it's children
                int totalChildHeight = 0;
                foreach (UINode child in children)
                    totalChildHeight += child.body.Height;

                if (yAlignment == Alignment.Centred)
                {
                    // if children are aligned to the middle then you need to add
                    // half of the remaining height
                    originY += body.Height / 2 - totalChildHeight / 2;
                }
                else// (alignment is positive)
                {   
                    // if the children are aligned to the bottom then
                    // add all of the remaining height minus the inner margin
                    originY += body.Height - totalChildHeight - innerMargin;
                }
            }

            // set all of it's children's positions
            for (int i = 0; i < children.Count(); i++)
            {
                children[i].updateYPositions(originY);
                originY += children[i].body.Height + childGap;
            }
        }
    }
    internal class UI9Patch : UINode
    {
        public Texture2D texture;
        public int textureMargin = 0;
        public int textureScale = 1;

        // intended constructor. Sets the texture to use and sets the
        // default color to white (since it's now used as a color mask)
        public UI9Patch(Texture2D textureIn)
        {
            texture = textureIn;
            color = Color.White;
        }

        // constructor for copying a template instance of a UI9Patch
        public UI9Patch(UI9Patch template) : base(template)
        {
            texture = template.texture;
            textureMargin = template.textureMargin;
            textureScale = template.textureScale;
        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            // calculate the relevant spacing information
            int posMargin = textureMargin*textureScale;// the size of the margin given the texture scale
            int rightPosOffset = body.Width - posMargin;// the X offset the rightmost patch has
            int bottomPosOffset = body.Height - posMargin;// the Y offset the bottommost patch has
            int rightTexOffset = texture.Width - textureMargin;// the X offset in the texture the rightmost patch has
            int bottomTexOffset = texture.Height - textureMargin;// the Y offset in the texture the bottommost patch has
            int innerWidth = body.Width - 2*posMargin;// the width of the horizontally centre patches
            int innerHeight = body.Height - 2*posMargin;// the height of the horizontally centre patches
            int innerTexWidth = texture.Width - 2*textureMargin;// the width of the horizontally centre patches in the texture
            int innerTexHeight = texture.Height - 2*textureMargin;// the height of the horizontally centre patches in the texture

            // handle the draw call of each patch
            spriteBatch.Draw(texture, // top left corner draw call
                new Rectangle(body.X, body.Y, posMargin, posMargin),
                new Rectangle(0,0,textureMargin,textureMargin),
            color);
            spriteBatch.Draw(texture, // top right corner draw call
                new Rectangle(body.X + rightPosOffset, body.Y, posMargin, posMargin),
                new Rectangle(rightTexOffset, 0, textureMargin, textureMargin),
            color);
            spriteBatch.Draw(texture, // bottom left corner draw call
                new Rectangle(body.X, body.Y + bottomPosOffset, posMargin, posMargin),
                new Rectangle(0, bottomTexOffset, textureMargin, textureMargin),
            color);
            spriteBatch.Draw(texture, // bottom right corner draw call
                new Rectangle(body.X + rightPosOffset, body.Y + bottomPosOffset, posMargin, posMargin),
                new Rectangle(rightTexOffset, bottomTexOffset, textureMargin, textureMargin),
            color);
            spriteBatch.Draw(texture, // top edge draw call
                new Rectangle(body.X + posMargin, body.Y, innerWidth, posMargin),
                new Rectangle(textureMargin, 0, innerTexWidth, textureMargin),
            color);
            spriteBatch.Draw(texture, // bottom edge draw call
                new Rectangle(body.X + posMargin, body.Y + bottomPosOffset, innerWidth, posMargin),
                new Rectangle(textureMargin, bottomTexOffset, innerTexWidth, textureMargin),
            color);
            spriteBatch.Draw(texture, // left edge draw call
                new Rectangle(body.X, body.Y + posMargin, posMargin, innerHeight),
                new Rectangle(0, textureMargin, textureMargin, innerTexHeight),
            color);
            spriteBatch.Draw(texture, // right edge draw call
                new Rectangle(body.X + rightPosOffset, body.Y + posMargin, posMargin, innerHeight),
                new Rectangle(rightTexOffset, textureMargin, textureMargin, innerTexHeight),
            color);
            spriteBatch.Draw(texture, // middle draw call
                new Rectangle(body.X + posMargin, body.Y + posMargin, innerWidth, innerHeight),
                new Rectangle(textureMargin, textureMargin, innerTexWidth, innerTexHeight),
            color);

            // draw each of it's children
            foreach (UINode child in children) child.Draw(spriteBatch, graphicsDevice);
        }
    }
    internal class UIText : UINode
    {
        SpriteFont font;
        public string text;
        public UIText setText(string value)
        {
            text = value;
            body.Size = font.MeasureString(text).ToPoint();
            return this; // incase this is used while constructing it
        }

        public UIText(SpriteFont font, string text)
        {
            this.font = font;
            setText(text);
            color = Color.Black;
        }
        // constructor for copying a template instance of a UIText
        public UIText(UIText template): base(template)
        {
            font = template.font;
            text = template.text;
        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            spriteBatch.DrawString(
                font,
                text,
                body.Location.ToVector2(),
                color
            );

            foreach (UINode child in children)
                child.Draw(spriteBatch, graphicsDevice);
        }
    }
    internal class Button(UINode UIelementIn)
    {
        public UINode UIelement = UIelementIn;
        public Action action = ()=>{};
    }
}