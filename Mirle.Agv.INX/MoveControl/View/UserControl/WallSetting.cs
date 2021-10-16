using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Model;

namespace Mirle.Agv.INX.View
{
    public partial class WallSetting : UserControl
    {
        private LocalData localData = LocalData.Instance;
        private ComputeFunction computeFunction = ComputeFunction.Instance;

        private WallSettingControl wallSetting = null;
        private MapData mapData = new MapData();

        private MapPicture mapPicture = null;

        public WallSetting(WallSettingControl wallSetting)
        {
            this.wallSetting = wallSetting;
            InitializeComponent();
            InitailMap();
        }

        public void InitailMap()
        {
            try
            {
                foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
                {
                    mapData.UpdateMaxMin(address.AGVPosition.Position);
                }

                mapData.SettingConfig(wallSetting.Config.MapBorderLength, wallSetting.Config.MapScale);

                mapPicture = new MapPicture(mapData, computeFunction);
                mapPicture.Location = new Point(0, 0);
                this.Controls.Add(mapPicture);
                mapPicture.SetPictureSize((mapData.MaxX - mapData.MinX), (mapData.MaxY - mapData.MinY), wallSetting.Config.MapScale);
                
                foreach (MapSection section in localData.TheMapInfo.AllSection.Values)
                {
                    mapPicture.DrawSection(section.FromAddress.AGVPosition.Position, section.ToAddress.AGVPosition.Position);
                }

                foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
                {
                    mapPicture.DrawAddress(address.AGVPosition.Position);
                }

                mapPicture.ShowMap();
                mapPicture.InitailAGVPicture();

                foreach (Wall wall in wallSetting.AllWall.Values)
                {
                    mapPicture.AddNewWall_Initail(wall);
                    cB_WallList.Items.Add(wall.ID);
                }

            }
            catch (Exception ex)
            {
                wallSetting.WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public void Timer_Update()
        {
            try
            {
                mapPicture.ShowAGV(localData.Real);

                if (mapPicture.GetLocateByWall)
                {
                    MapPosition locateStart = mapPicture.LocateStart;
                    MapPosition locateEnd = mapPicture.LocateEnd;

                    if (locateStart != null && locateEnd != null)
                    {
                        mapPicture.GetLocateByWall = false;

                        tB_StartX.Text = locateStart.X.ToString("0");
                        tB_StartY.Text = locateStart.Y.ToString("0");
                        tB_EndX.Text = locateEnd.X.ToString("0");
                        tB_EndY.Text = locateEnd.Y.ToString("0");
                    }
                }
            }
            catch
            {
            }
        }

        private void button_ShowAll_Click(object sender, EventArgs e)
        {
            mapPicture.ShowAll();
            mapPicture.GetLocateByWall = false;
        }

        private void button_HideAll_Click(object sender, EventArgs e)
        {
            mapPicture.HideAll();
            mapPicture.GetLocateByWall = false;
        }

        private bool Check_WallList(ref string wallID)
        {
            if (wallSetting.AllWall.ContainsKey(cB_WallList.Text))
            {
                wallID = cB_WallList.Text;
                return true;
            }
            else
                return false;
        }

        private void button_ShowThisWallData_Click(object sender, EventArgs e)
        {
            string wallID = "";

            if (Check_WallList(ref wallID))
            {
                tB_ID.Text = wallSetting.AllWall[wallID].ID;
                tB_StartX.Text = wallSetting.AllWall[wallID].Start.X.ToString("0");
                tB_StartY.Text = wallSetting.AllWall[wallID].Start.Y.ToString("0");
                tB_EndX.Text = wallSetting.AllWall[wallID].End.X.ToString("0");
                tB_EndY.Text = wallSetting.AllWall[wallID].End.Y.ToString("0");
                tB_Distance.Text = wallSetting.AllWall[wallID].Distance.ToString("0");

                if (mapPicture.Type != "all")
                    mapPicture.OnlyShowThisWall(wallID);
            }
            else
            {
                tB_ID.Text = "";
                tB_StartX.Text = "";
                tB_StartY.Text = "";
                tB_EndX.Text = "";
                tB_EndY.Text = "";
                tB_Distance.Text = "";
            }

            mapPicture.GetLocateByWall = false;
        }

        private void button_OnlyShowThisWall_Click(object sender, EventArgs e)
        {
            string wallID = "";

            if (Check_WallList(ref wallID))
                mapPicture.OnlyShowThisWall(wallID);

            mapPicture.GetLocateByWall = false;
        }

        private void button_AddNewWall_Click(object sender, EventArgs e)
        {
            tB_ID.Text = "";
            tB_StartX.Text = "";
            tB_StartY.Text = "";
            tB_EndX.Text = "";
            tB_EndY.Text = "";
            tB_Distance.Text = "";
            mapPicture.HideAll();
            mapPicture.GetLocateByWall = true;
        }

        private void button_DeleteThisWall_Click(object sender, EventArgs e)
        {
            button_DeleteThisWall.Enabled = false;
            button_Send.Enabled = false;
            button_WriteCSV.Enabled = false;

            string wallID = "";

            if (Check_WallList(ref wallID))
            {
                if (MessageBox.Show(String.Concat("是否確定刪除 ", wallID, " ?"), "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    wallSetting.DeleteThisWall(wallID);
                    mapPicture.DeleteThisWall(wallID);
                    cB_WallList.Items.Remove((object)wallID);

                    tB_ID.Text = "";
                    tB_StartX.Text = "";
                    tB_StartY.Text = "";
                    tB_EndX.Text = "";
                    tB_EndY.Text = "";
                    tB_Distance.Text = "";
                }
            }

            mapPicture.GetLocateByWall = false;

            button_DeleteThisWall.Enabled = true;
            button_Send.Enabled = true;
            button_WriteCSV.Enabled = true;
        }

        private bool CheckWallDataOK(ref string id, ref MapPosition start, ref MapPosition end, ref double distance)
        {
            double temp;

            if (tB_ID.Text == "")
                return false;

            id = tB_ID.Text;

            if (!double.TryParse(tB_StartX.Text, out temp))
                return false;

            start.X = temp;

            if (!double.TryParse(tB_StartY.Text, out temp))
                return false;

            start.Y = temp;

            if (!double.TryParse(tB_EndX.Text, out temp))
                return false;

            end.X = temp;

            if (!double.TryParse(tB_EndY.Text, out temp))
                return false;

            end.Y = temp;

            if (!double.TryParse(tB_Distance.Text, out temp))
                return false;

            distance = temp;

            return true;
        }

        private void button_Send_Click(object sender, EventArgs e)
        {
            button_DeleteThisWall.Enabled = false;
            button_Send.Enabled = false;
            button_WriteCSV.Enabled = false;

            mapPicture.GetLocateByWall = false;

            MapPosition start = new MapPosition();
            MapPosition end = new MapPosition();
            string id = "";
            double distnace = 0;
            double angle = 0;

            if (CheckWallDataOK(ref id, ref start, ref end, ref distnace))
            {
                if (MessageBox.Show("是否確定修改/新增?", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    angle = computeFunction.ComputeAngle(start, end);

                    Wall wall = new Wall(id, start, end, angle, distnace, wallSetting.Config.SleepTime);

                    if (wallSetting.AllWall.ContainsKey(wall.ID))
                    {       // changeInfo.
                        wallSetting.ChangeWallData(wall);
                        mapPicture.ChangeWallData(wall);
                    }
                    else
                    {       // add.
                        wallSetting.AddNewWall(wall);
                        cB_WallList.Items.Add(wall.ID);
                        mapPicture.AddNewWall(wall);
                    }
                }
            }

            button_DeleteThisWall.Enabled = true;
            button_Send.Enabled = true;
            button_WriteCSV.Enabled = true;
        }

        private void button_WriteCSV_Click(object sender, EventArgs e)
        {
            button_DeleteThisWall.Enabled = false;
            button_Send.Enabled = false;
            button_WriteCSV.Enabled = false;

            wallSetting.WriteCSV();
            mapPicture.GetLocateByWall = false;
            MessageBox.Show("存檔成功!");

            button_DeleteThisWall.Enabled = true;
            button_Send.Enabled = true;
            button_WriteCSV.Enabled = true;
        }
    }
}
