using Mirle.Agv.INX.Control;
using Mirle.Agv.INX.Controller;
using Mirle.Agv.INX.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mirle.Agv.INX.View
{
    public partial class MainForm : Form
    {
        private MainFlowHandler mainFlow;
        private DrawMapData drawMapData;
        private LocalData localData = LocalData.Instance;
        private UcVehicleImage agv = null;
        private ComputeFunction computeFunction = ComputeFunction.Instance;

        #region Form.
        private MoveControlForm moveControlForm;
        private MIPCViewForm mipcViewForm;
        private ProFaceForm proFaceForm;
        private BigSizeWallSettingForm bigSizeWallSettingForm;
        #endregion

        private EnumLoginLevel lastLoginLvel = EnumLoginLevel.User;
        private EnumAutoState lastAutoState = EnumAutoState.Manual;

        private List<string> changeColorAddresList = new List<string>();
        private List<string> addressList = new List<string>();
        private object lockAddressListChange = new object();

        public Panel GetPanel
        {
            get
            {
                return panel_Map;
            }
        }

        public Size GetMapSize
        {
            get
            {
                return pB_Map.Size;
            }
        }

        public DrawMapData GetDrawMapData
        {
            get
            {
                return drawMapData;
            }
        }

        public Bitmap GetMapImage
        {
            get
            {
                return drawMapData.ObjectAndSection;
            }
        }

        public Dictionary<string, AddressPicture> GetAllAddressPicture
        {
            get
            {
                return drawMapData.AllAddressPicture;
            }
        }

        public MainForm(MainFlowHandler mainFlow)
        {
            this.mainFlow = mainFlow;
            InitializeComponent();
            pB_Map.SizeMode = PictureBoxSizeMode.AutoSize;
            panel_Map.AutoScroll = true;
            InitialMap();
            InitialForm();
            //人機畫面ToolStripMenuItem_Click(null, new EventArgs());
        }

        #region Map.
        private void InitialMap()
        {
            drawMapData = new DrawMapData();

            SetPicureSize();
            DrawObject();
            DrawSection();
            DrawAddress();
            DrawAGV();
        }

        public void SetPicureSize()
        {
            try
            {
                foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
                {
                    drawMapData.UpdateMaxAndMin(address.AGVPosition.Position);
                }

                drawMapData.SetMapBorderLength(localData.MapConfig.MapBorderLength, localData.MapConfig.MapScale);
            }
            catch (Exception ex)
            {
                mainFlow.WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private void DrawSection(string sectionID, EnumSectionAction type)
        {
            if (type == EnumSectionAction.None)
                return;

            if (drawMapData.SectionData.ContainsKey(sectionID) && drawMapData.SectionData[sectionID].Type != type)
            {
                drawMapData.Graphics_ObjectAndSection.DrawLine(drawMapData.AllPen[type], drawMapData.SectionData[sectionID].X1, drawMapData.SectionData[sectionID].Y1,
                                                                                drawMapData.SectionData[sectionID].X2, drawMapData.SectionData[sectionID].Y2);

                drawMapData.Graphics_SectionID.DrawString(sectionID, new Font("標楷體", 14), Brushes.Black,
                           new PointF((float)(drawMapData.SectionData[sectionID].X1 + drawMapData.SectionData[sectionID].X2) / 2,
                                      (float)(drawMapData.SectionData[sectionID].Y1 + drawMapData.SectionData[sectionID].Y2) / 2 + 10));


                drawMapData.SectionData[sectionID].Type = type;

                pB_Map.Image = drawMapData.ObjectAndSection;
            }
        }

        private void DrawObject()
        {
            try
            {
                drawMapData.ObjectAndSection = new Bitmap(drawMapData.MapSize.Width, drawMapData.MapSize.Height);
                drawMapData.Graphics_ObjectAndSection = Graphics.FromImage(drawMapData.ObjectAndSection);
                drawMapData.Graphics_ObjectAndSection.Clear(Color.White);

                Point[] pointArray;

                int portLength = 0;
                int portToAddressDistance = 0;

                switch (localData.MainFlowConfig.AGVType)
                {
                    default:
                        portLength = 300;
                        portToAddressDistance = 800;
                        break;
                }

                double angle = 0;
                MapPosition center;
                foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
                {
                    if (address.LoadUnloadDirection != EnumStageDirection.None)
                    {
                        pointArray = new Point[4];

                        switch (address.LoadUnloadDirection)
                        {
                            case EnumStageDirection.Right:
                                angle = address.AGVPosition.Angle + 90;
                                break;
                            case EnumStageDirection.Left:
                                angle = address.AGVPosition.Angle - 90;
                                break;
                            default:
                                return;
                        }

                        center = new MapPosition(address.AGVPosition.Position.X + portToAddressDistance * Math.Cos(angle / 180 * Math.PI),
                                                 address.AGVPosition.Position.Y + portToAddressDistance * Math.Sin(angle / 180 * Math.PI));
                        pointArray[0] = new Point((int)drawMapData.TransferX(center.X - portLength),
                                                  (int)drawMapData.TransferY(center.Y - portLength));
                        pointArray[1] = new Point((int)drawMapData.TransferX(center.X + portLength),
                                                  (int)drawMapData.TransferY(center.Y - portLength));
                        pointArray[2] = new Point((int)drawMapData.TransferX(center.X + portLength),
                                                  (int)drawMapData.TransferY(center.Y + portLength));
                        pointArray[3] = new Point((int)drawMapData.TransferX(center.X - portLength),
                                                  (int)drawMapData.TransferY(center.Y + portLength));

                        drawMapData.Graphics_ObjectAndSection.DrawPolygon(Pens.Black, pointArray);

                        int size = 10;
                        Font font = new Font("微軟正黑體", size);
                        string addressName = address.AddressName != "" ? address.AddressName : "Port";


                        SizeF s = drawMapData.Graphics_ObjectAndSection.MeasureString(addressName, font);

                        float x = (float)(drawMapData.TransferX(center.X) - s.Width / 2);
                        float y = (float)(drawMapData.TransferY(center.Y) - s.Height / 2);

                        drawMapData.Graphics_ObjectAndSection.DrawString(addressName, font, Brushes.Black, new PointF(x, y));
                    }
                    else if (address.ChargingDirection != EnumStageDirection.None)
                    {
                        pointArray = new Point[4];

                        switch (address.ChargingDirection)
                        {
                            case EnumStageDirection.Right:
                                angle = address.AGVPosition.Angle + 90;
                                break;
                            case EnumStageDirection.Left:
                                angle = address.AGVPosition.Angle - 90;
                                break;
                            default:
                                return;
                        }

                        center = new MapPosition(address.AGVPosition.Position.X + portToAddressDistance * Math.Cos(angle / 180 * Math.PI),
                                                 address.AGVPosition.Position.Y + portToAddressDistance * Math.Sin(angle / 180 * Math.PI));
                        pointArray[0] = new Point((int)drawMapData.TransferX(center.X - portLength),
                                                  (int)drawMapData.TransferY(center.Y - portLength));
                        pointArray[1] = new Point((int)drawMapData.TransferX(center.X + portLength),
                                                  (int)drawMapData.TransferY(center.Y - portLength));
                        pointArray[2] = new Point((int)drawMapData.TransferX(center.X + portLength),
                                                  (int)drawMapData.TransferY(center.Y + portLength));
                        pointArray[3] = new Point((int)drawMapData.TransferX(center.X - portLength),
                                                  (int)drawMapData.TransferY(center.Y + portLength));

                        drawMapData.Graphics_ObjectAndSection.DrawPolygon(Pens.Red, pointArray);

                        int size = 8;
                        Font font = new Font("微軟正黑體", size);
                        string addressName = address.AddressName != "" ? address.AddressName : "Charger";

                        SizeF s = drawMapData.Graphics_ObjectAndSection.MeasureString(addressName, font);

                        float x = (float)(drawMapData.TransferX(center.X) - s.Width / 2);
                        float y = (float)(drawMapData.TransferY(center.Y) - s.Height / 2);

                        drawMapData.Graphics_ObjectAndSection.DrawString(addressName, font, Brushes.Black, new PointF(x, y));
                    }
                }

                /*
                for (int i = 0; i < localData.TheMapInfo.ObjectDataList.Count; i++)
                {
                    pointArray = new Point[localData.TheMapInfo.ObjectDataList[i].PositionList.Count];
                    centerX = 0;
                    centerY = 0;

                    for (int j = 0; j < localData.TheMapInfo.ObjectDataList[i].PositionList.Count; j++)
                    {
                        pointArray[j] = new Point((int)drawMapData.TransferX(localData.TheMapInfo.ObjectDataList[i].PositionList[j].X),
                                                  (int)drawMapData.TransferY(localData.TheMapInfo.ObjectDataList[i].PositionList[j].Y));

                        centerX += pointArray[j].X;
                        centerY += pointArray[j].Y;
                    }

                    centerX /= localData.TheMapInfo.ObjectDataList[i].PositionList.Count;
                    centerY /= localData.TheMapInfo.ObjectDataList[i].PositionList.Count;

                    drawMapData.Graphics_ObjectAndSection.DrawPolygon(Pens.Black, pointArray);

                    int size = 12;
                    Font font = new Font("微軟正黑體", size);

                    SizeF s = drawMapData.Graphics_ObjectAndSection.MeasureString(localData.TheMapInfo.ObjectDataList[i].Name, font);

                    float x = centerX - s.Width / 2;
                    float y = centerY - s.Height / 2;

                    drawMapData.Graphics_ObjectAndSection.DrawString(localData.TheMapInfo.ObjectDataList[i].Name, font, Brushes.Black, new PointF(x, y));
                }
                */
            }
            catch (Exception ex)
            {
                mainFlow.WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public void DrawSection()
        {
            try
            {
                drawMapData.SectionIDBitmap = new Bitmap(drawMapData.MapSize.Width, drawMapData.MapSize.Height);
                drawMapData.Graphics_SectionID = Graphics.FromImage(drawMapData.SectionIDBitmap);
                drawMapData.Graphics_SectionID.Clear(Color.White);

                drawMapData.SectionIDPicture = new PictureBox();
                drawMapData.SectionIDPicture.Size = new Size(drawMapData.MapSize.Width, drawMapData.MapSize.Height);
                drawMapData.SectionIDPicture.BackColor = Color.Transparent;

                foreach (MapSection section in localData.TheMapInfo.AllSection.Values)
                {
                    drawMapData.SetSectionData(section);
                    DrawSection(section.Id, EnumSectionAction.Idle);
                }

                pB_Map.Size = drawMapData.MapSize;
                pB_Map.Image = drawMapData.ObjectAndSection;

                drawMapData.SectionIDPicture.Image = drawMapData.SectionIDBitmap;
            }
            catch (Exception ex)
            {
                mainFlow.WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        private void DrawAddressID(MapAddress address)
        {
            float y = drawMapData.TransferY(address.AGVPosition.Position.Y) + localData.MapConfig.AddressWidth / 2 + 1;

            int size = 8;

            Font font = new Font("微軟正黑體", size);

            SizeF s = drawMapData.Graphics_ObjectAndSection.MeasureString(address.Id, font);

            float x = drawMapData.TransferX(address.AGVPosition.Position.X) - s.Width / 2;

            drawMapData.Graphics_ObjectAndSection.FillRectangle(new SolidBrush(Color.White), x, y, s.Width, s.Height);

            drawMapData.Graphics_ObjectAndSection.DrawString(address.Id, font, Brushes.Black, new PointF(x, y));
        }

        public void DrawAddress()
        {
            try
            {
                AddressPicture addressPicture;

                foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
                {
                    addressPicture = new AddressPicture(address, localData.MapConfig.AddressWidth, localData.MapConfig.AddressLineWidth,
                                   drawMapData.TransferX(address.AGVPosition.Position.X), drawMapData.TransferY(address.AGVPosition.Position.Y));

                    DrawAddressID(address);
                    drawMapData.AllAddressPicture.Add(address.Id, addressPicture);
                }

                foreach (MapAddress address in localData.TheMapInfo.AllAddress.Values)
                {
                    pB_Map.Controls.Add(drawMapData.AllAddressPicture[address.Id]);
                    drawMapData.AllAddressPicture[address.Id].PB_Address.MouseDoubleClick += AddressPictureClickEvent;
                }
            }
            catch (Exception ex)
            {
                mainFlow.WriteLog(3, "", String.Concat("Exception : ", ex.ToString()));
            }
        }

        public void DrawAGV()
        {
            agv = new UcVehicleImage(drawMapData);
            pB_Map.Controls.Add(agv);
        }

        private MoveCommandData lastMoveCommand = null;
        private int lastSectionLineIndex = 0;
        private int lastGetReserveIndex = 0;

        private void InitialMoveCommandSectionColor(MoveCommandData temp)
        {
            for (int i = 0; i < temp.ReserveList.Count; i++)
                DrawSection(temp.ReserveList[i].SectionID, EnumSectionAction.NotGetReserve);
        }

        private void RefreshMoveCommandSectionColor()
        {
            while (lastGetReserveIndex < lastMoveCommand.ReserveList.Count && lastMoveCommand.ReserveList[lastGetReserveIndex].GetReserve)
            {
                DrawSection(lastMoveCommand.ReserveList[lastGetReserveIndex].SectionID, EnumSectionAction.GetReserve);
                lastGetReserveIndex++;
            }

            for (; lastSectionLineIndex < lastMoveCommand.IndexOflisSectionLine; lastSectionLineIndex++)
                DrawSection(lastMoveCommand.SectionLineList[lastSectionLineIndex].Section.Id, EnumSectionAction.Idle);
        }

        private void ClearMoveCommandSectionColor()
        {
            lastGetReserveIndex = 0;
            lastSectionLineIndex = 0;

            foreach (string sectionID in localData.TheMapInfo.AllSection.Keys)
            {
                DrawSection(sectionID, EnumSectionAction.Idle);
            }
        }

        private void SetSectionColor()
        {
            MoveCommandData temp = localData.MoveControlData.MoveCommand;

            if (lastMoveCommand != temp)
            {
                ClearMoveCommandSectionColor();

                if (temp != null)
                    InitialMoveCommandSectionColor(temp);

                lastMoveCommand = temp;
            }
            else
            {
                if (temp != null)
                    RefreshMoveCommandSectionColor();
            }
        }
        #endregion

        #region Form相關.
        private void InitialForm()
        {
            moveControlForm = new MoveControlForm(mainFlow.MoveControl);
            mipcViewForm = new MIPCViewForm(mainFlow.MipcControl);
            proFaceForm = new ProFaceForm(mainFlow, this);
            bigSizeWallSettingForm = new BigSizeWallSettingForm(mainFlow.MoveControl.WallSetting);

            if (!localData.SimulateMode)
            {
                proFaceForm.ShowForm();
                proFaceForm.Size = new Size(800, 600);
                proFaceForm.Location = new Point(0, 0);
                //proFaceForm.TopMost = true;
            }
        }

        private void MoveControl_Click(object sender, EventArgs e)
        {
            try
            {
                if (moveControlForm == null)
                    moveControlForm = new MoveControlForm(mainFlow.MoveControl);

                moveControlForm.ShowForm();
            }
            catch { }
        }

        private void MIPC_Click(object sender, EventArgs e)
        {
            try
            {
                mipcViewForm.ShowForm();
            }
            catch { }
        }

        private void ShowProface()
        {
            try
            {
                if (proFaceForm == null)
                    proFaceForm = new ProFaceForm(mainFlow, this);

                proFaceForm.ShowForm();
                proFaceForm.Size = new Size(800, 600);
                proFaceForm.Location = new Point(0, 0);
                //proFaceForm.TopMost = true;
            }
            catch { }
        }

        private void 人機畫面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowProface();
        }
        #endregion

        private bool lastProfaceShow = false;

        private Stopwatch showProfaceTimer = new Stopwatch();

        private double showProfaceTime = 10 * 60 * 1000;

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!localData.SimulateMode)
            {
                bool profaceShow = proFaceForm != null && proFaceForm.Visible;

                if (profaceShow != lastProfaceShow)
                {
                    profaceShow = lastProfaceShow;

                    if (!profaceShow)
                        showProfaceTimer.Restart();
                }
                else if (!profaceShow)
                {
                    if (showProfaceTimer.ElapsedMilliseconds > showProfaceTime)
                        ShowProface();
                }
            }

            label_TestString.Text = localData.TestString;
            LoginLevelAction();
            AutoManaulAction();
            agv.UpdateLocate(localData.Real);
            button_SetPosition.Enabled = (localData.AutoManual == EnumAutoState.Manual && localData.MoveControlData.MoveCommand == null);

            if (setPositionThread != null && setPositionThread.IsAlive)
            {
                label_LocateStatus.Text = "重定位~";
                label_LocateStatus.ForeColor = Color.DarkBlue;
            }
            else if (localData.MoveControlData.LocateControlData.SlamLocateOK)
            {
                label_LocateStatus.Text = "定位OK";
                label_LocateStatus.ForeColor = Color.Green;
            }
            else
            {
                label_LocateStatus.Text = "定位NG";
                label_LocateStatus.ForeColor = Color.Red;
            }

            pB_Alarm.BackColor = (localData.MIPCData.HasAlarm ? Color.Red : Color.Transparent);
            pB_Warn.BackColor = (localData.MIPCData.HasWarn ? Color.Yellow : Color.Transparent);
            pB_reservestatus.BackColor = (localData.ReserveStatus_forView ? Color.Green : Color.Transparent);

            SetSectionColor();
        }

        #region 登入/登出.
        private void LoginLevelAction()
        {
            lastLoginLvel = localData.LoginLevel;
        }
        #endregion

        #region AutoManualAction.
        private void AutoManaulAction()
        {
            EnumAutoState newState = localData.AutoManual;

            if (newState != lastAutoState)
            {
                switch (newState)
                {
                    case EnumAutoState.Auto:
                        button_AutoManual.Enabled = true;
                        button_AutoManual.BackColor = Color.Green;
                        button_AutoManual.Text = EnumAutoState.Auto.ToString();
                        break;
                    case EnumAutoState.Manual:
                        button_AutoManual.Enabled = true;
                        button_AutoManual.BackColor = Color.Red;
                        button_AutoManual.Text = EnumAutoState.Manual.ToString();
                        break;
                    case EnumAutoState.PreAuto:
                        button_AutoManual.BackColor = Color.DarkRed;
                        button_AutoManual.Text = EnumAutoState.PreAuto.ToString();
                        button_AutoManual.Enabled = false;
                        break;
                    default:
                        button_AutoManual.Enabled = false;
                        break;
                }
            }

            lastAutoState = newState;
        }


        private void button_Alarm_Click(object sender, EventArgs e)
        {
            mainFlow.ResetAlarm();
        }
        #endregion

        #region address add/clear/send相關.
        private void AddressPictureClickEvent(object sender, MouseEventArgs e)
        {
            System.Windows.Forms.Control control = ((System.Windows.Forms.Control)sender).Parent;
            AddressPicture addressPicture = (AddressPicture)control;

            try
            {
                if (localData.AutoManual == EnumAutoState.Manual)
                {
                    lock (lockAddressListChange)
                    {
                        drawMapData.AllAddressPicture[addressPicture.AddressID].ChangeAddressBackColor();
                        MovingAddresList.Items.Add(addressPicture.AddressID);
                        if (!changeColorAddresList.Contains(addressPicture.AddressID))
                            changeColorAddresList.Add(addressPicture.AddressID);

                        addressList.Add(addressPicture.AddressID);
                    }

                    moveControlForm.SetAddress(addressPicture.AddressID);
                }
            }
            catch { }
        }

        private void button_SendToMoveControlDebug_Click(object sender, EventArgs e)
        {
            try
            {
                if (localData.AutoManual == EnumAutoState.Manual)
                {
                    moveControlForm.RecieveAddresListFromMainForm(addressList);
                    moveControlForm.ShowForm();
                }
            }
            catch { }
        }

        private void button_ClearAddressList_Click(object sender, EventArgs e)
        {
            try
            {
                lock (lockAddressListChange)
                {
                    for (int i = 0; i < changeColorAddresList.Count; i++)
                        drawMapData.AllAddressPicture[changeColorAddresList[i]].ResetAddressColor();

                    changeColorAddresList = new List<string>();
                    MovingAddresList.Items.Clear();
                    addressList = new List<string>();
                }
            }
            catch { }
        }
        #endregion

        private void 關閉ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!localData.SimulateMode &&
                (localData.MoveControlData.MoveCommand != null ||
                 localData.LoadUnloadData.LoadUnloadCommand != null))
            {
                MessageBox.Show("走行/取放命令中, 請先停止命令再關閉程式");
                return;
            }

            moveControlForm.Close();
            mipcViewForm.Close();

            mainFlow.CloseMainFlowHandler();

            try
            {
                Application.Exit();
                Environment.Exit(Environment.ExitCode);
                this.Close();
            }
            catch { }
        }

        private string setPositionAddressID = "";
        private Thread setPositionThread = null;

        private void SetPositionThread()
        {
            mainFlow.MoveControl.LocateControl.SetSLAMPositionByAddressID(setPositionAddressID, 500, 5);
        }

        private void button_SetPosition_Click(object sender, EventArgs e)
        {
            try
            {
                if (localData.AutoManual == EnumAutoState.Manual)
                {
                    if (addressList.Count > 0)
                    {
                        if (setPositionThread == null || !setPositionThread.IsAlive)
                        {
                            setPositionAddressID = addressList[addressList.Count - 1];
                            setPositionThread = new Thread(SetPositionThread);
                            setPositionThread.Start();
                        }

                        button_ClearAddressList_Click(null, null);
                    }
                }
            }
            catch { }
        }

        private void btnAutoManual_Click(object sender, EventArgs e)
        {
            mainFlow.ChangeAutoManual_MainForm();
        }

        private void button_BuzzOff_Click(object sender, EventArgs e)
        {
            localData.MIPCData.BuzzOff = true;
        }

        private void 牆壁設定ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bigSizeWallSettingForm == null)
                bigSizeWallSettingForm = new BigSizeWallSettingForm(mainFlow.MoveControl.WallSetting);

            bigSizeWallSettingForm.ShowForm();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Size = new Size(1961, 1162);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Application.Exit();
                Environment.Exit(Environment.ExitCode);
                this.Close();
            }
            catch { }
        }
    }
}