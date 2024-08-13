using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WintabDN;
using System.Drawing;
using Manual.API;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace Manual.Core;

//BROWN
public static class PenPressureExtensions
{
    public static void RemoveWTPacketEventHandler(this CWintabData data, EventHandler<MessageReceivedEventArgs> handler_I)
    {
        MessageEvents.MessageReceived -= handler_I;
    }

}
public static partial class PenPressure
{


    //brown espantoso
    static private CWintabContext m_logContext = null;
    static private CWintabData m_wtData = null;

    static private UInt32 m_maxPkts = 1;   // max num pkts to capture/display at a time

    static private Int32 m_pkX = 0;
    static private Int32 m_pkY = 0;
    static private UInt32 m_pressure = 0;
    static private UInt32 m_pkTime = 0;
    static private UInt32 m_pkTimeLast = 0;

    static private Point m_lastPoint = new Point(0, 0);




    static void TraceMsg(string message)
    {
        if (Settings.instance.DebugMode)
            Output.Log(message);
    }

    public static bool InitSystemDataCapture(bool ctrlSysCursor_I = true)
    {
        try
        {
            if (!IsDllAvailable("Wintab32.dll"))
                return false;          


            if (!CWintabInfo.IsWintabAvailable())
                return false;

            // Close context from any previous test.
            CloseCurrentContext();

           // TraceMsg("Pen Pressure actived...\n");

            m_logContext = OpenTestSystemContext(ctrlSysCursor_I);

            if (m_logContext == null)
            {
                TraceMsg("Test_DataPacketQueueSize: FAILED OpenTestSystemContext - bailing out...\n");
                return false;
            }

            CreateDataObject(m_logContext);
        }
        catch (DllNotFoundException dllEx)
        {
            return false;
        }
        catch (Exception ex)
        {
            //TraceMsg("Digital Pen not recognized " + ex.ToString());
            return false;
        }
        return true;
    }
    private static bool IsDllAvailable(string dllName)
    {
        IntPtr handle = LoadLibrary(dllName);
        if (handle == IntPtr.Zero)
        {
            return false;
        }
        FreeLibrary(handle);
        return true;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);


    public static void CloseCurrentContext()
    {
        try
        {
           // TraceMsg("Pen Pressure desactived...\n");
            if (m_wtData != null)
            {
                m_wtData.RemoveWTPacketEventHandler(MyWTPacketEventHandler);
                m_wtData = null;
            }
            if (m_logContext != null)
            {
                m_logContext.Close();
                m_logContext = null;
            }

        }
        catch (Exception ex)
        {
            if(Enabled)
              System.Windows.MessageBox.Show(ex.ToString());
        }
    }

    private static CWintabContext OpenTestSystemContext(bool ctrlSysCursor = true)
    {
        bool status = false;
        CWintabContext logContext = null;

        try
        {
            // Get the default system context.
            // Default is to receive data events.
            logContext = CWintabInfo.GetDefaultSystemContext(ECTXOptionValues.CXO_MESSAGES);

            // Set system cursor if caller wants it.
            if (ctrlSysCursor)
            {
                logContext.Options |= (uint)ECTXOptionValues.CXO_SYSTEM;
            }
            else
            {
                logContext.Options &= ~(uint)ECTXOptionValues.CXO_SYSTEM;
            }

            if (logContext == null)
            {
                TraceMsg("FAILED to get default wintab context.\n");
                return null;
            }

            // ----------------------------------------------------------------------
            // Modify the tablet extents to set what part of the tablet is used.
            Rectangle newTabletInRect = new Rectangle();
            Rectangle newTabletOutRect = new Rectangle();

            SetTabletExtents(ref logContext, newTabletInRect, newTabletOutRect);

            // ----------------------------------------------------------------------
            // Modify the system extents to control where cursor is allowed to move on desktop.
            Rectangle newScreenRect = new Rectangle();

            SetSystemExtents(ref logContext, newScreenRect);

            // Open the context, which will also tell Wintab to send data packets.
            status = logContext.Open();

            //TraceMsg("Context Open: " + (status ? "PASSED [ctx=" + logContext.HCtx + "]" : "FAILED") + "\n");

            //TraceMsg("PenPressure Enabled");
        }
        catch (Exception ex)
        {
            TraceMsg("OpenTestDigitizerContext ERROR: " + ex.ToString());
        }

        return logContext;
    }
    private static void SetTabletExtents(ref CWintabContext logContext, Rectangle newTabletInRect, Rectangle newTabletOutRect)
    {
        if (logContext == null)
        {
            throw new NullReferenceException("Oops - null wintab context");
        }

        if (newTabletInRect.Width != 0 && newTabletInRect.Height != 0)
        {
            logContext.InOrgX = newTabletInRect.X;
            logContext.InOrgY = newTabletInRect.Y;
            logContext.InExtX = newTabletInRect.Width;
            logContext.InExtY = newTabletInRect.Height;
        }

        if (newTabletOutRect.Width != 0 && newTabletOutRect.Height != 0)
        {
            logContext.OutOrgX = newTabletOutRect.X;
            logContext.OutOrgY = newTabletOutRect.Y;
            logContext.OutExtX = newTabletOutRect.Width;
            logContext.OutExtY = newTabletOutRect.Height;
        }

        if (logContext.OutExtY > 0)
        {
            // In Wintab, the tablet origin is lower left.  Move origin to upper left
            // so that it coincides with screen origin.
            logContext.OutExtY = -logContext.OutExtY;
        }
    }
    private static void SetSystemExtents(ref CWintabContext logContext, Rectangle newScreenRect)
    {
        if (logContext == null)
        {
            throw new NullReferenceException("Oops - null wintab context");
        }

        if (newScreenRect.Width != 0 && newScreenRect.Height != 0)
        {
            logContext.SysOrgX = newScreenRect.X;
            logContext.SysOrgY = newScreenRect.Y;
            logContext.SysExtX = newScreenRect.Width;
            logContext.SysExtY = newScreenRect.Height;
        }
    }

    private static void CreateDataObject(CWintabContext logContext_I)
    {
        if (logContext_I == null)
        {
            throw new NullReferenceException("Oops - NULL wintab context when setting data handler");
        }

        // Create a data object and set its WT_PACKET handler.
        m_wtData = new CWintabData(m_logContext);
        m_wtData.SetWTPacketEventHandler(MyWTPacketEventHandler);
    }

    public static void MyWTPacketEventHandler(object? sender_I, MessageReceivedEventArgs eventArgs_I)
    {
        if (m_wtData == null)
        {
            return;
        }

        try
        {
            if (m_maxPkts == 1)
            {
                uint pktID = (uint)eventArgs_I.Message.WParam;
                WintabPacket pkt = m_wtData.GetDataPacket((uint)eventArgs_I.Message.LParam, pktID);

                if (pkt.pkContext != 0)
                {
                    m_pkX = pkt.pkX;
                    m_pkY = pkt.pkY;
                    m_pressure = pkt.pkNormalPressure;

                    //Trace.WriteLine("SCREEN: pkX: " + pkt.pkX + ", pkY:" + pkt.pkY + ", pressure: " + pkt.pkNormalPressure);

                    m_pkTime = pkt.pkTime;




                    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------- LA CHICHA
                    OnPAcketReceived(pkt);




                    Point clientPoint = new Point(m_pkX, m_pkY);

                    if (m_lastPoint.Equals(new Point(0, 0)))
                    {
                        m_lastPoint = clientPoint;
                        m_pkTimeLast = m_pkTime;
                    }

                    float width = (float)m_pressure / (float)MaxPressure;
                    int penIdx = (int)(width * 10) - 1; if (penIdx < 0) { penIdx = 0; }
                    //Debug.WriteLine($"pressure: {m_pressure}; width:{m_pen.Width}; penIdx: {penIdx}");


                    //PRESIONADO
                    if (m_pressure > 0)
                    {
                        //m_graphics.DrawLine(m_drawPens[penIdx], clientPoint, m_lastPoint);
                        //AppModel.mainW.SetMessage($"CLIENT:   X: {clientPoint.X}, Y:{clientPoint.Y}, Pressure: {m_pressure}");

                    }

                    m_lastPoint = clientPoint;
                    m_pkTimeLast = m_pkTime;
                }

            }
        }
        catch (Exception ex)
        {
            throw new Exception("FAILED to get packet data: " + ex.ToString());
        }
    }

}
//












public static partial class PenPressure
{
    public static PenInfo CurrentPenInfo { get; set; }
    public static int MaxPressure;

    static bool _enabled = false;
    public static bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                if (!_enabled && value)
                    _ = InitSystemDataCaptureAsync(); // Ejecuta de manera asíncrona
                else if (_enabled)
                    _ = CloseCurrentContextAsync(); // Ejecuta de manera asíncrona

                _enabled = value;
            }
        }
    }

    private static async Task InitSystemDataCaptureAsync()
    {
        await Task.Run(() =>
        {
            bool opened = PenPressure.InitSystemDataCapture();
            if (opened)
                MaxPressure = CWintabInfo.GetMaxPressure();
            else
                Enabled = false;
        });
    }

    private static async Task CloseCurrentContextAsync()
    {
        await Task.Run(() =>
        {
            PenPressure.CloseCurrentContext();
        });
    }
    public static void Enable() => Enabled = true;
    public static void Disable() => Enabled = false;


    static void OnPAcketReceived(WintabPacket pkt)
    {
        //use
        var realPen = new PenInfo(pkt);
        if (realPen.ButtonState == PenInfo.PenButton.ClickDown)
        {
            var X = realPen.X;
            var Y = realPen.Y;
            var pressure = realPen.Pressure;
            //...
        }

        // display data mode
        CurrentPenInfo = realPen;
        bool showData = false;
        if (showData && Settings.instance.DebugMode)
            AppModel.mainW.SetMessage(PenInfo.PacketToString(pkt));

    }



}



//translate WintabPacket to x64
public struct PenInfo
{
    public int X { get; set; }
    public int Y { get; set; }
    public uint Pressure { get; set; }
    public uint Raising { get; set; }

    public PenButton ButtonState = PenButton.None;

    public PenInfo(WintabPacket packet)
    {
        X = packet.pkY;
        Y = packet.pkZ;
        Pressure = packet.pkTangentPressure;
        Raising = packet.pkNormalPressure;

        ButtonState = GetPenButton(packet.pkX);
    }
    public static PenButton GetPenButton(int pkX)
    {
        switch (pkX)
        {
            case 131072:
                return PenButton.ClickDown;
            case 65536:
                return PenButton.ClickUp;
            case 131074:
                return PenButton.TopButtonDown;
            case 65538:
                return PenButton.TopButtonUp;
            case 131073:
                return PenButton.BottomButtonDown;
            case 65537:
                return PenButton.BottomButtonUp;
            default: // 0
                return PenButton.None;
        }
    }

    public override string ToString()
    {
        return
            $"X: {X} " +
            $"Y: {Y} " +
            $"Pressure: {Pressure} " +
            $"Raising: {Raising}" +
            $"ButtonState: {ButtonState}";
    }


    public static string PacketToString(WintabPacket pkt)
    {
        return $"Received WT_PACKET event" +
            $"  pkX: { pkt.pkX}" +
            $"  pkY: {pkt.pkY}" +
            $"  pxz: {pkt.pkZ}" +
            $"  normalPressure: {pkt.pkNormalPressure}" +
            $"  pkTime: {pkt.pkTime}" +
            $"  tangentPressure: {pkt.pkTangentPressure}" +          
            $"  context: {pkt.pkContext}" +
            $"  changed: {pkt.pkChanged}" +
            $"  status: {pkt.pkStatus}" +
            $"  buttons: {pkt.pkButtons} " +
            $"  cursor: {pkt.pkCursor}".ToSingleLine();
    }

    public enum PenButton
    {
        None,

        ClickDown,
        ClickUp,

        TopButtonDown,
        TopButtonUp,

        BottomButtonDown,
        BottomButtonUp,
    }

}
