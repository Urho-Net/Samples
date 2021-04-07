// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
// Copyright (c) 2008-2021 the Urho3D project.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using Urho;
using System;
using System.Collections.Generic;

namespace FlappyUrho
{
    public class Score3D : Component
    {
        const float DIGIT_SPACING = 1.5f;
        uint score;
        bool alignRight;
        List<Node> digitNodes = new  List<Node>();


        public override void OnAttachedToNode(Node node)
        {
            base.OnAttachedToNode(node);
            
            Node.Rotation = new Quaternion(180.0f ,Vector3.Up);

            SetAlignRight(false);
            AddDigit();
            SetScore(0);
        }

        public void SetScore(uint _score)
        {
            score = _score;
            String scoreString = score.ToString();

            while (scoreString.Length != digitNodes.Count)
            {
                if (digitNodes.Count < scoreString.Length)
                    AddDigit();
                else if (digitNodes.Count > scoreString.Length)
                    RemoveDigit();
            }

            //Update score graphics
            for (int i = 0; i < digitNodes.Count; i++)
            {
                Node n = digitNodes[i];

                StaticModel digitModel = n.GetComponent<StaticModel>();
                int t = ((int)(score / (uint)(Math.Pow(10, i))) % 10);

                digitModel.Model = GetModel(t.ToString());
                digitModel.Material = GetMaterial("Emerald");
            }
        }

        public void SetAlignRight(bool _alignRight)
        {
            alignRight = _alignRight;

            Node.Position = GetRootPosition();

            for (int i = 0; i < digitNodes.Count; i++)
            {
                Node n = digitNodes[i];
                n.Position = GetDigitTargetPosition(i);
            }

        }

        Vector3 GetDigitTargetPosition(int index)
        {
            return Vector3.Right * DIGIT_SPACING * index;
        }

        Vector3 GetRootPosition()
        {
            var graphics = Application.Graphics;
            float ratio = (float)graphics.Width / graphics.Height;
            float scoreWidth = DIGIT_SPACING * digitNodes.Count - 1;
            return new Vector3(alignRight ? 9.5f * ratio : -9.5f * ratio + scoreWidth, 8.0f, -5.0f);
        }

        void AddDigit()
        {
            Node digitNode = Node.CreateChild("Digit");
            digitNode.Position = GetDigitTargetPosition(digitNodes.Count);
            digitNode.CreateComponent<StaticModel>();
            digitNodes.Add(digitNode);

            Node.Position = GetRootPosition();
        }

        void RemoveDigit()
        {
            if (digitNodes.Count > 0)
            {
                Node lastDigit = digitNodes[digitNodes.Count - 1];
                lastDigit.RemoveAllComponents();
                lastDigit.Remove();
                digitNodes.RemoveAt(digitNodes.Count - 1);

                Node.Position = GetRootPosition();
            }
        }

        Model GetModel(String name)
        {
            var cache = Application.ResourceCache;
            return cache.GetModel("Models/" + name + ".mdl");
        }

        Material GetMaterial(String name)
        {
            var cache = Application.ResourceCache;
            return cache.GetMaterial("Materials/" + name + ".xml");
        }


    }

}