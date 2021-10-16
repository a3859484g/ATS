using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mirle.Agv.INX.Model;
using Mirle.Agv.INX.Controller;

namespace Mirle.Agv.INX.View
{
    public partial class MapPicture : UserControl
    {
        private Bitmap bMap = null;
        private Graphics gph = null;
        private MapData mapData = null;

        private Dictionary<string, PictureBox> allWallPicture = new Dictionary<string, PictureBox>();
        private Dictionary<string, Bitmap> allBitMap = new Dictionary<string, Bitmap>();
        private List<PictureBox> wallPictureList = new List<PictureBox>();
        private Dictionary<string, int> wallIDToIndex = new Dictionary<string, int>();

        private ComputeFunction computeFunction = null;

        //private PictureBox agvAddTarget = null;
        private PictureBox onFirstPBTarget = null;
        private PictureBox onlyShowPicutreBox = null;

        private Pen wallPen = new Pen(Color.Black, 3);

        private PictureBox agv;

        public string Type { get; set; } = "all";

        public MapPicture(MapData mapData, ComputeFunction computeFunction)
        {
            this.computeFunction = computeFunction;
            this.mapData = mapData;
            InitializeComponent();
            pB_Picture.SizeMode = PictureBoxSizeMode.AutoSize;
            panel1.AutoScroll = true;
        }

        public void InitailAGVPicture()
        {
            agv = new PictureBox();
            agv.Size = new Size(10, 10);
            agv.BackColor = Color.Green;
            agv.Visible = false;
            pB_Picture.Controls.Add(agv);
        }

        public void SetPictureSize(double width, double length, double mapscale)
        {
            //panel1.BackColor = Color.Black;
            pB_Picture.Size = new Size((int)(width * mapscale), (int)(length * mapscale));
            bMap = new Bitmap(pB_Picture.Size.Width, pB_Picture.Size.Height);
            gph = Graphics.FromImage(bMap);
            gph.Clear(Color.White);
        }

        public void DrawSection(MapPosition start, MapPosition end)
        {
            if (bMap == null)
                return;

            gph.DrawLine(Pens.Blue, (float)mapData.TransferX(start.X), (float)mapData.TransferY(start.Y),
                                    (float)mapData.TransferX(end.X), (float)mapData.TransferY(end.Y));
        }

        public void DrawAddress(MapPosition address, int size = 2)
        {
            if (size < 2)
                size = 2;

            float x = (float)mapData.TransferX(address.X);
            float y = (float)mapData.TransferY(address.Y);

            PointF[] xPt = new PointF[4]
            {
                new PointF(x-size,y-size),
                new PointF(x-size,y+size),
                new PointF(x+size,y+size),
                new PointF(x+size,y-size)
            };

            gph.DrawPolygon(Pens.Red, xPt);
            gph.FillPolygon(new SolidBrush(Color.Red), xPt);
        }

        public void DrawSlamAddress(MapPosition address, int size = 2)
        {
            if (size < 2)
                size = 2;

            float x = (float)mapData.TransferX(address.X);
            float y = (float)mapData.TransferY(address.Y);

            PointF[] xPt = new PointF[3]
            {
                new PointF(x,y-size),
                new PointF(x-size,y+size),
                new PointF(x+size,y+size)
            };

            gph.DrawPolygon(Pens.Blue, xPt);
            gph.FillPolygon(new SolidBrush(Color.Red), xPt);
        }

        public void ShowMap()
        {
            pB_Picture.Image = bMap;
            onlyShowPicutreBox = new PictureBox();
            onlyShowPicutreBox.Size = new Size(pB_Picture.Size.Width, pB_Picture.Size.Height);
            onlyShowPicutreBox.BackColor = Color.Transparent;
        }

        public void ShowAGV(MapAGVPosition now)
        {
            if (now == null)
                agv.Visible = false;
            else
            {
                agv.Location = new Point((int)(mapData.TransferX(now.Position.X) - agv.Width / 2), (int)(mapData.TransferY(now.Position.Y) - agv.Height / 2));
                agv.Visible = true;
            }
        }

        private void UpdateMaxMin(MapPosition position, ref double minX, ref double maxX, ref double minY, ref double maxY)
        {
            if (position.X > maxX)
                maxX = position.X;
            else if (position.X < minX)
                minX = position.X;

            if (position.Y > maxY)
                maxY = position.Y;
            else if (position.Y < minY)
                minY = position.Y;
        }

        private void ComputeWallData(Wall wall, ref MapPosition wallStart, ref MapPosition wallEnd,
                                     ref MapPosition corner1, ref MapPosition corner2, ref MapPosition corner3, ref MapPosition corner4)
        {
            double angle = computeFunction.ComputeAngle(wall.Start, wall.End) / 180 * Math.PI + Math.PI / 2;

            corner1 = new MapPosition(mapData.TransferX(wall.Start.X + Math.Cos(angle) * wall.Distance), mapData.TransferY(wall.Start.Y + Math.Sin(angle) * wall.Distance));
            corner2 = new MapPosition(mapData.TransferX(wall.Start.X + Math.Cos(Math.PI + angle) * wall.Distance), mapData.TransferY(wall.Start.Y + Math.Sin(Math.PI + angle) * wall.Distance));
            corner3 = new MapPosition(mapData.TransferX(wall.End.X + Math.Cos(Math.PI + angle) * wall.Distance), mapData.TransferY(wall.End.Y + Math.Sin(Math.PI + angle) * wall.Distance));
            corner4 = new MapPosition(mapData.TransferX(wall.End.X + Math.Cos(angle) * wall.Distance), mapData.TransferY(wall.End.Y + Math.Sin(angle) * wall.Distance));
            wallStart = new MapPosition(mapData.TransferX(wall.Start.X), mapData.TransferY(wall.Start.Y));
            wallEnd = new MapPosition(mapData.TransferX(wall.End.X), mapData.TransferY(wall.End.Y));
        }

        private Bitmap GetBitmapByWallData(Wall wall)
        {
            MapPosition wallStart = new MapPosition();
            MapPosition wallEnd = new MapPosition();
            MapPosition corner1 = new MapPosition();
            MapPosition corner2 = new MapPosition();
            MapPosition corner3 = new MapPosition();
            MapPosition corner4 = new MapPosition();

            ComputeWallData(wall, ref wallStart, ref wallEnd, ref corner1, ref corner2, ref corner3, ref corner4);

            Bitmap tempBitmap = new Bitmap(pB_Picture.Size.Width, pB_Picture.Size.Height);

            Graphics graphics = Graphics.FromImage(tempBitmap);

            graphics.Clear(Color.White);

            PointF[] xPt = new PointF[4]
            {
                     new PointF((float)corner1.X,(float)corner1.Y),
                     new PointF((float)corner2.X,(float)corner2.Y),
                     new PointF((float)corner3.X,(float)corner3.Y),
                     new PointF((float)corner4.X,(float)corner4.Y)
            };

            graphics.DrawString(wall.ID, new Font("標楷體", 14), Brushes.Black,
                           new PointF((float)(wallStart.X + wallEnd.X) / 2, (float)(wallStart.Y + wallEnd.Y) / 2 + 10));

            graphics.DrawPolygon(Pens.LightGray, xPt);

            graphics.DrawLine(wallPen, (float)wallStart.X, (float)wallStart.Y, (float)wallEnd.X, (float)wallEnd.Y);

            tempBitmap.MakeTransparent(Color.White);
            return tempBitmap;
        }

        private PictureBox GetPictureByWallData(Wall wall)
        {
            PictureBox picture = new PictureBox();
            picture.Size = new Size(pB_Picture.Size.Width, pB_Picture.Size.Height);
            Bitmap tempBitmap = GetBitmapByWallData(wall);

            if (allBitMap.ContainsKey(wall.ID))
                allBitMap[wall.ID] = tempBitmap;
            else
                allBitMap.Add(wall.ID, tempBitmap);

            picture.Image = tempBitmap;
            picture.Location = new Point(0, 0);
            picture.BackColor = Color.Transparent;
            return picture;
        }

        public void AddNewWall_Initail(Wall wall)
        {
            if (!allWallPicture.ContainsKey(wall.ID))
            {
                PictureBox picture = GetPictureByWallData(wall);

                if (allWallPicture.Count == 0)
                {
                    onFirstPBTarget = picture;
                    pB_Picture.Controls.Add(picture);
                    wallIDToIndex.Add(wall.ID, 0);
                }
                else
                {
                    wallPictureList[wallPictureList.Count - 1].Controls.Add(picture);
                    wallIDToIndex.Add(wall.ID, wallIDToIndex.Count);
                }

                wallPictureList.Add(picture);
                allWallPicture.Add(wall.ID, picture);
            }
        }

        public void HidWallByID(string id)
        {
            if (allWallPicture.ContainsKey(id))
            {
                allWallPicture[id].Visible = false;
            }
        }

        public void ShowAll()
        {
            try
            {
                if (onFirstPBTarget != null)
                {
                    pB_Picture.Controls.Remove(onFirstPBTarget);
                    onFirstPBTarget = null;
                }

                if (wallPictureList.Count > 0)
                {
                    pB_Picture.Controls.Add(wallPictureList[0]);
                    onFirstPBTarget = wallPictureList[0];
                }

                Type = "all";
            }
            catch
            {
            }
        }

        public void HideAll()
        {
            try
            {
                if (onFirstPBTarget != null)
                {
                    pB_Picture.Controls.Remove(onFirstPBTarget);
                    onFirstPBTarget = null;
                }

                Type = "zero";
            }
            catch
            {
            }
        }

        public void AddNewWall(Wall wall)
        {
            try
            {
                string tempType = Type;

                HideAll();

                PictureBox picture = GetPictureByWallData(wall);

                if (wallPictureList.Count > 0)
                {
                    wallPictureList[wallPictureList.Count - 1].Controls.Add(picture);
                    allWallPicture.Add(wall.ID, picture);
                }
                else
                    allWallPicture.Add(wall.ID, picture);

                wallPictureList.Add(picture);
                wallIDToIndex.Add(wall.ID, wallIDToIndex.Count);

                if (tempType == "all")
                    ShowAll();
                else
                    OnlyShowThisWall(wall.ID);
            }
            catch
            {
            }
        }

        public void ChangeWallData(Wall wall)
        {
            try
            {
                Bitmap tempBitmap = GetBitmapByWallData(wall);
                allBitMap[wall.ID] = tempBitmap;
                int index = wallIDToIndex[wall.ID];
                allWallPicture[wall.ID].Image = tempBitmap;

                if (Type != "all")
                    OnlyShowThisWall(wall.ID);
            }
            catch
            {
            }
        }

        public void OnlyShowThisWall(string wallID)
        {
            try
            {

                HideAll();

                onlyShowPicutreBox.Image = allBitMap[wallID];
                pB_Picture.Controls.Add(onlyShowPicutreBox);
                onFirstPBTarget = onlyShowPicutreBox;

                Type = "";
            }
            catch
            {
            }
        }

        public void DeleteThisWall(string wallID)
        {
            try
            {
                HideAll();

                int index = wallIDToIndex[wallID];

                if (index < wallPictureList.Count - 1)
                { // 有下一個 需要斷連結.
                    if (index > 0)
                    { // 不是第一個.
                        wallPictureList[index - 1].Controls.Add(wallPictureList[index + 1]);
                        wallPictureList[index - 1].Controls.Remove(wallPictureList[index]);
                    }

                    wallPictureList[index].Controls.Remove(wallPictureList[index + 1]);
                }
                else
                {
                    if (index > 0)
                    { // 有前一個.
                        wallPictureList[index - 1].Controls.Remove(wallPictureList[index]);
                    }
                }

                allWallPicture.Remove(wallID);
                wallIDToIndex.Remove(wallID);
                wallPictureList.RemoveAt(index);

                foreach (string temp in allWallPicture.Keys)
                {
                    if (wallIDToIndex[temp] > index)
                        wallIDToIndex[temp] -= 1;
                }

                ShowAll();
            }
            catch
            {

            }
        }

        private bool getLocateByWall = false;

        public bool GetLocateByWall
        {
            get
            {
                return getLocateByWall;
            }

            set
            {
                LocateStart = null;
                LocateEnd = null;
                getLocateByWall = value;
            }
        }

        public MapPosition LocateStart { get; set; } = null;
        public MapPosition LocateEnd { get; set; } = null;

        private void LocateCheck_MouseDown(object sender, MouseEventArgs e)
        {
            if (getLocateByWall && LocateStart == null)
                LocateStart = new MapPosition(mapData.AntiTransferX(e.Location.X), mapData.AntiTransferY(e.Location.Y));
        }

        private void LocateCheck_MouseUp(object sender, MouseEventArgs e)
        {
            if (getLocateByWall && LocateEnd == null)
                LocateEnd = new MapPosition(mapData.AntiTransferX(e.Location.X), mapData.AntiTransferY(e.Location.Y));
        }
    }
}
