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
using System.Threading;

namespace Mirle.Agv.INX.View
{
    public partial class PIOHistoryForm : UserControl
    {
        private LocalData localData = LocalData.Instance;

        private int timeLabelStartX = 10;

        private int historyStartX = 70;
        private int historyWidth = 580;

        private int 需要分上下層Width = 70;

        private int startY = 10;
        private int heigh = 30;

        private Label timeLabel;
        private List<Label> inputNameLabelList = new List<Label>();
        private List<int> inputStartXList = new List<int>();
        private List<Label> outputNameLabelList = new List<Label>();
        private List<int> outputStartXList = new List<int>();

        private List<string> pioInputNameList = new List<string>();
        private List<string> pioOutputNameList = new List<string>();

        private int showStartY = 0;
        private int width = 0;

        private int maxOfPIOHisotryCount = 50;

        private int lastPIOHisotryCount = 0;

        private List<PIOTimeAndValueUserControl> allData = new List<PIOTimeAndValueUserControl>();

        public PIOHistoryForm(List<string> pioInputNameList, List<string> pioOutputNameList)
        {
            InitializeComponent();
            this.pioInputNameList = pioInputNameList;
            this.pioOutputNameList = pioOutputNameList;
            this.pioOutputNameList = pioOutputNameList;
            InitialTitleLabel();
        }

        private string GetStringByTag(string tag)
        {
            return localData.GetProfaceString(localData.Language, tag);
        }

        private string GetStringByTag(EnumProfaceStringTag tag)
        {
            return localData.GetProfaceString(localData.Language, tag.ToString());
        }

        public void ChangeLanguage()
        {
            label_CommandID.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.CommandID), " : ");
            label_AddressID.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.AddressID), " : ");
            label_CommandResult.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.Result), " : ");

            label_StartTime.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.StartTime), " : ");
            label_EndTime.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.EndTime), " : ");

            //timeLabel.Text = GetStringByTag( EnumProfaceStringTag.)
        }

        private void InitialTitleLabel()
        {
            #region 初始化標題.
            panel.AutoScroll = true;

            width = historyWidth / (pioInputNameList.Count + pioOutputNameList.Count + 1);

            timeLabel = new Label();
            timeLabel.Text = "時間";
            timeLabel.Font = new System.Drawing.Font("標楷體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            timeLabel.Size = new Size(historyStartX - timeLabelStartX + 1, heigh + 1);
            timeLabel.MouseDown += label_MouseDown;
            timeLabel.MouseUp += panel_MouseUp;
            timeLabel.MouseMove += label_MouseMove;
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;
            timeLabel.BorderStyle = BorderStyle.FixedSingle;
            panel.Controls.Add(timeLabel);

            bool 上下兩層 = false;

            if (width >= 需要分上下層Width)
                timeLabel.Location = new Point(timeLabelStartX, startY);
            else
            {
                timeLabel.Location = new Point(timeLabelStartX, startY + heigh);
                上下兩層 = true;
            }

            int temp = historyStartX;

            if (上下兩層)
                historyStartX -= (width / 2);

            bool 上 = true;

            Label tempLabel;

            for (int i = 0; i < pioInputNameList.Count; i++)
            {
                tempLabel = new Label();
                tempLabel.Text = pioInputNameList[i];
                tempLabel.Font = new System.Drawing.Font("標楷體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
                tempLabel.Size = new Size((上下兩層 ? width * 2 : width) + 1, heigh + 1);
                tempLabel.TextAlign = ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.MouseDown += label_MouseDown;
                tempLabel.MouseUp += panel_MouseUp;
                tempLabel.MouseMove += label_MouseMove;
                inputNameLabelList.Add(tempLabel);
                panel.Controls.Add(tempLabel);

                if (上)
                    tempLabel.Location = new Point(historyStartX, startY);
                else
                    tempLabel.Location = new Point(historyStartX, startY + heigh);

                inputStartXList.Add(historyStartX + (上下兩層 ? (width / 2) : 0) - timeLabelStartX);

                if (上下兩層)
                    上 = !上;

                historyStartX += width;
            }

            historyStartX += width;

            for (int i = 0; i < pioOutputNameList.Count; i++)
            {
                tempLabel = new Label();
                tempLabel.Text = pioOutputNameList[i];
                tempLabel.Font = new System.Drawing.Font("標楷體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
                tempLabel.Size = new Size((上下兩層 ? width * 2 : width) + 1, heigh + 1);
                tempLabel.TextAlign = ContentAlignment.MiddleCenter;
                tempLabel.BorderStyle = BorderStyle.FixedSingle;
                tempLabel.MouseDown += label_MouseDown;
                tempLabel.MouseUp += panel_MouseUp;
                tempLabel.MouseMove += label_MouseMove;
                outputNameLabelList.Add(tempLabel);
                panel.Controls.Add(tempLabel);

                if (上)
                    tempLabel.Location = new Point(historyStartX, startY);
                else
                    tempLabel.Location = new Point(historyStartX, startY + heigh);

                if (上下兩層)
                    上 = !上;

                outputStartXList.Add(historyStartX + (上下兩層 ? (width / 2) : 0) - timeLabelStartX);

                historyStartX += width;
            }

            showStartY = startY + heigh * (上下兩層 ? 2 : 1);
            #endregion

            PIOTimeAndValueUserControl tempPIOData;
            int tempY = showStartY;

            for (int i = 0; i < maxOfPIOHisotryCount; i++)
            {
                tempPIOData = new PIOTimeAndValueUserControl();
                tempPIOData.InitialLabel(timeLabel.Size, width, heigh, inputStartXList, outputStartXList);

                tempPIOData.Location = new Point(timeLabelStartX, tempY);
                tempPIOData.Visible = false;
                tempPIOData.OjbectMouseDown += TempPIOData_OjbectMouseDown;
                tempPIOData.OjbectMouseMove += TempPIOData_OjbectMouseMove;
                tempPIOData.OjbectMouseUp += TempPIOData_OjbectMouseUp; ;

                panel.Controls.Add(tempPIOData);
                allData.Add(tempPIOData);
                tempY += heigh;
            }
        }

        private void TempPIOData_OjbectMouseUp(object sender, EventArgs e)
        {
            PIOFormMouseUp();
        }

        private void TempPIOData_OjbectMouseMove(object sender, Point e)
        {
            PIOFormMouseMove(e);
        }

        private void TempPIOData_OjbectMouseDown(object sender, Point e)
        {
            PIOFormMouseDown(e);
        }

        public void SetCommandResult(LoadUnloadCommandData command)
        {
            #region 取放資訊.
            label_StartTimeValue.Text = command.CommandStartTime.ToString("HH:mm:ss");
            label_EndTimeValue.Text = command.CommandEndTime.ToString("HH:mm:ss");

            if (localData.TheMapInfo.AllAddress.ContainsKey(command.AddressID))
            {
                label_AddressID.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.AddressID), " : ");
                label_AddressIDValue.Text = command.AddressID;
            }
            else
            {
                label_AddressID.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.方向), " : ");
                label_AddressIDValue.Text = GetStringByTag(command.StageDirection.ToString());
            }

            if (command.NeedPIO)
                label_PIOResult.Text = GetStringByTag(command.PIOResult.ToString());
            else
                label_PIOResult.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.PIO), GetStringByTag(EnumProfaceStringTag.不使用));

            if (command.ReadFail)
                label_CSTID.Text = GetStringByTag(GetStringByTag(EnumProfaceStringTag.CstReadFail));
            else
                label_CSTID.Text = command.CSTID;

            label_AutoManual.Text = GetStringByTag(command.CommandAutoManual.ToString());
            label_Action.Text = GetStringByTag(command.Action.ToString());

            label_CommandResultValue.Text = GetStringByTag(command.CommandResult.ToString());
            label_ErrorCodeValue.Text = command.ErrorCode.ToString();

            switch (localData.MainFlowConfig.AGVType)
            {
                case EnumAGVType.UMTC:
                    AlignmentValueData data = command.CommandStartAlignmentValue;

                    if (!command.UsingAlignmentValue)
                        label_AlignmentData.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.啟用補正), GetStringByTag(EnumProfaceStringTag.不使用));
                    else if (data == null || !data.AlignmentVlaue)
                        label_AlignmentData.Text = String.Concat(GetStringByTag(EnumProfaceStringTag.AlignmentValue), GetStringByTag(EnumProfaceStringTag.Error));
                    else
                    {
                        label_AlignmentData.Text =
                            String.Concat(GetStringByTag(EnumProfaceStringTag.AlignmentValue), " [ ",
                                          GetStringByTag(EnumProfaceStringTag.Alignment_P), " : ", data.P.ToString("0.0"), " ,",
                                          GetStringByTag(EnumProfaceStringTag.Alignment_Y), " : ", data.Y.ToString("0.0"), " ,",
                                          GetStringByTag(EnumProfaceStringTag.Alignment_Theta), " : ", data.Theta.ToString("0.0"), " ,",
                                          GetStringByTag(EnumProfaceStringTag.Alignment_Z), " : ", data.Z.ToString("0.0"), " ]");
                    }

                    break;
            }
            #endregion

            SetPIOHistory(command.PIOHistory);
        }

        private void SetPIOHistory(List<PIODataAndTime> pioHistory)
        {
            int lastCount = lastPIOHisotryCount;

            lastPIOHisotryCount = pioHistory.Count;

            if (lastPIOHisotryCount > maxOfPIOHisotryCount)
                lastPIOHisotryCount = maxOfPIOHisotryCount;

            int i = 0;

            for (; i < lastPIOHisotryCount; i++)
            {
                allData[i].Visible = true;
                allData[i].SetData(pioHistory[i], pioHistory[0].Time);
            }

            for (; i < lastCount; i++)
                allData[i].Visible = false;
        }

        private Point moveMapLocateStart = new Point(0, 0);
        private bool mouseDown = false;

        private void label_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PIOFormMouseDown(new Point(e.Location.X + ((Label)sender).Location.X, e.Location.Y + ((Label)sender).Location.Y));
        }

        private void label_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PIOFormMouseMove(new Point(e.Location.X + ((Label)sender).Location.X, e.Location.Y + ((Label)sender).Location.Y));
        }

        private void panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PIOFormMouseMove(e.Location);
        }

        private void panel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PIOFormMouseDown(e.Location);
        }

        private void panel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PIOFormMouseUp();
        }

        private void PIOFormMouseDown(Point locate)
        {
            moveMapLocateStart = new Point(locate.X, locate.Y);
            mouseDown = true;
        }

        private void PIOFormMouseUp()
        {
            mouseDown = false;
        }

        private bool moving = false;

        private void PIOFormMouseMove(Point locate)
        {
            if (mouseDown && !moving)
            {
                moving = true;
                int deltaX = locate.X - moveMapLocateStart.X;
                int deltaY = locate.Y - moveMapLocateStart.Y;

                moveMapLocateStart = locate;

                panel.AutoScrollPosition =
                    new Point(-panel.AutoScrollPosition.X - deltaX,
                              -panel.AutoScrollPosition.Y - deltaY);

                moving = false;
            }
        }
    }
}
