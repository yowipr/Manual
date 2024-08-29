using Manual.API;
using Manual.Core;
using ManualToolkit.Specific;
using ManualToolkit.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;
using static Manual.Core.ActionHistory;

using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Controls.Primitives;
using System.Windows.Automation.Peers;


namespace Manual.Editors.Displays.Launcher;

/// <summary>
/// Lógica de interacción para W_AssetStore.xaml
/// </summary>
public partial class W_Launcher : W_WindowContent
{
    //just for debug test
    private void Reload(object sender, RoutedEventArgs e)
    {

        var path = "C:\\Users\\YO\\AppData\\Local\\Manual\\Dependencies\\new_ComfyUI_windows_portable_nvidia_cu121_or_cpu.7z";
        var dependenciesPath = "C:\\Users\\YO\\AppData\\Local\\Manual\\Dependencies";


        FileManager.Extract7zFile(path, dependenciesPath);
        return;

        window.Hide();
        M_Window.NewShow(new W_Launcher(true), "", M_Window.TabButtonsType._X);
        window.Close();
    }








    public W_Launcher(bool enableSetup)
    {
        InitializeComponent();
        DataContext = AppModel.launcher;
        isSetup = enableSetup;

        Loaded += W_AssetStore_Loaded;

        sectionGrid.Opacity = 0;
        downloadMenu.Opacity = 0;
        downloadMenu.Visibility = Visibility.Collapsed;



        CommonStart();


    }

    bool isSetup;

    void CommonStart()
    {

#if DEBUG
        debugReload.Visibility = Visibility.Visible;
#else
    debugReload.Visibility = Visibility.Collapsed;
#endif


        AppModel.launcher.CurrentSection = AppModel.launcher.l_store;
        AppModel.launcher.ui = this;

        SetL_StoreTemplate();


    }

    private void W_AssetStore_Loaded(object sender, RoutedEventArgs e)
    {
        start();
        if(isSetup)
         window.ContentRendered += Window_ContentRenderedStartup;
        else
            window.ContentRendered += Window_ContentRendered;
    }
    private void Window_ContentRendered(object? sender, EventArgs e)
    {
        //downloadMenu.Opacity = 1;
        //downloadMenu.Visibility = Visibility.Visible;
        //userIcon.Opacity = 1;
        //userIcon.Visibility = Visibility.Visible;
    }




    //PREPARING
    private void Window_ContentRenderedStartup(object? sender, EventArgs e)
    {
        if(AppModel.IsForceLogin)
          maybeLaterLoginBtn.Visibility = Visibility.Collapsed;

        setupGrid.Visibility = Visibility.Visible;
        var b = AnimationLibrary.BreatheOpacity(mainText);
        b.Begin();

        var mov = AnimationLibrary.Translate(mainText, toX: 0, toY: 0, duration: 2, fromY: 20);
        mov.Begin();

        var op = AnimationLibrary.Opacity(this, to: 1, duration: 1);
        AnimationLibrary.OnFinalized(op, () =>
        {
            DoAction.Do(StartingSetup, 3);
        });
        op.Begin();
    }



    //---------------------------------------------------------------------------------------------------- PREPARING
    void StartingSetup()
    {
        userIcon.Visibility = Visibility.Collapsed;
        sectionGrid.Opacity = 0;
        sectionGrid.IsEnabled = false;

        bool enable = false;     
        App.InitializeShell();


        var mov = AnimationLibrary.Translate(mainText, toX: 0, toY: -20, duration: 2);
        mov.Begin();

        var op = AnimationLibrary.Opacity(mainText, to: 0, duration: 1);
        AnimationLibrary.OnFinalized(op, Setup_signIn);
        op.Begin();


    }

    //--------------------------------------------------------------------------------------------------- SETUP LOGIN
    void Setup_signIn()
    {
        backButton.Visibility = Visibility.Collapsed;
        var op0 = AnimationLibrary.Opacity(backButton, to: 0, duration: 1);
        op0.Begin();


        setupStep = 1;

        setupGrid.Visibility = Visibility.Collapsed;

        setupGrid_signIn.Opacity = 0;

        mainText_signIn2.Opacity = 0;
        signIn_grid.Opacity = 0;

        setupGrid_signIn.Visibility = Visibility.Visible;

        var mov = AnimationLibrary.Translate(mainText_signIn, toX: 0, toY: 0, duration: 2, fromY: 20);
        mov.Begin();

        var op = AnimationLibrary.Opacity(setupGrid_signIn, to: 1, duration: 1);
        op.Begin();


        var mov2 = AnimationLibrary.Translate(mainText_signIn2, toX: 0, toY: 0, duration: 2, fromY: 20);
        mov2.Begin();

        var op2 = AnimationLibrary.Opacity(mainText_signIn2, to: 0.7, duration: 1);
        AnimationLibrary.OnFinalized(op2, start);
        op2.Begin();


        void start()
        {
            //buttons
            var op3 = AnimationLibrary.Opacity(signIn_grid, to: 1, duration: 1);
            op3.Begin();
        }

    }

    public void Setup_ComfyStart()
    {
       // MessageBox.Show("Setup_ComfyStart");

        skipedComfy = false;

        var op000 = AnimationLibrary.Opacity(sectionGrid, to: 0, duration: 1);
        op000.Begin();
        sectionGrid.IsEnabled = false;

        backButton.Opacity = 0;
        backButton.Visibility = Visibility.Visible;
        var op00 = AnimationLibrary.Opacity(backButton, to: 1, duration: 1);
        op00.Begin();
        backButton.IsEnabled = true;


        var op = AnimationLibrary.Opacity(setupGrid_signIn, to: 0, duration: 1);
        op.Begin();

        setupGrid_comfy.Opacity = 0;
        setupGrid_comfy.Visibility = Visibility.Visible;
        var op0 = AnimationLibrary.Opacity(setupGrid_comfy, to: 1, duration: 1);
        AnimationLibrary.OnFinalized(op0, Setup_Comfy);
        op0.Begin();


        //user icon
        userIcon.Opacity = 0;
        userIcon.Visibility = Visibility.Visible;
        var op2 = AnimationLibrary.Opacity(userIcon, to: 1, duration: 0.5);
        op2.Begin();

        //bottom sec
        bottomSec.Opacity = 0;
        bottomSec.Visibility = Visibility.Visible;
        var opp = AnimationLibrary.Opacity(bottomSec, to: 1, duration: 0.5);
        opp.Begin();



        //TEXTS

        comfy_grid.Opacity = 0;
        mainText_comfy2.Opacity = 0;

        var mov = AnimationLibrary.Translate(mainText_comfy, toX: 0, toY: 0, duration: 2, fromY: 20);
        mov.Begin();


        var mov2 = AnimationLibrary.Translate(mainText_comfy2, toX: 0, toY: 0, duration: 2, fromY: 20);
        mov2.Begin();

        var op3 = AnimationLibrary.Opacity(mainText_comfy2, to: 0.7, duration: 1);
        AnimationLibrary.OnFinalized(op3, start);
        op3.Begin();


        void start()
        {
            //buttons
            var op4 = AnimationLibrary.Opacity(comfy_grid, to: 1, duration: 1);
            op4.Begin();
        }

    }

    private void Button_Click_Link(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(W_Login.url);
        copiedText.Visibility = Visibility.Visible;
    }

    private void Button_ClickLogin(object sender, RoutedEventArgs e)
    {
        WebManager.OPEN(W_Login.url);
        DoAction.Do(() =>
        {
            loginBtn.Visibility = Visibility.Collapsed;
            loginLoading.Visibility = Visibility.Visible;
            btnLink.Visibility = Visibility.Visible;
            btnToken.Visibility = Visibility.Visible;
        }, 0.5);
    }





    //----------------------------------------------------------------------------------------------------- SETUP COMFY
    void Setup_Comfy()
    {
        var op = AnimationLibrary.Opacity(comfy_grid, to: 1, duration: 1);
        op.Begin();

        var op0 = AnimationLibrary.Opacity(sectionGrid, to: 0, duration: 1);
        sectionGrid.IsEnabled = false;
        op0.Begin();


        setupStep = 2;
        setupGrid_signIn.Visibility = Visibility.Collapsed;

        setupGrid_comfy.Visibility = Visibility.Visible;
    }

    private void Click_comfyLocate(object sender, RoutedEventArgs e)
    {
        var succed = AppModel.V_LocateComfy();
        if(succed) Setup_comfyFinal();
    }


    private void Click_comfyDownload(object sender, RoutedEventArgs e)
    {
        downloadComfyBtn.Content = "Downloading ComfyUI...";
        downloadComfyBtn.IsEnabled = false;

        Task.Factory.StartNew(()=>ComfyUIServer.InstallComfy(finalizedComfy));
        Setup_comfyFinal();
    }
    void finalizedComfy()
    {
        if(AppModel.launcher.Downloads.Count <= 1)
            Setup_StartManual();
    }


    bool skipedComfy = false;
    void Setup_comfyFinal()
    {
        downloadMenu.Opacity = 0;
        downloadMenu.Visibility = Visibility.Visible;
        var op0 = AnimationLibrary.Opacity(downloadMenu, to: 1, duration: 1);
        op0.Begin();

        var op = AnimationLibrary.Opacity(setupGrid_comfy, to: 0, duration: 1);

        if(!skipedComfy)
           AnimationLibrary.OnFinalized(op, Setup_ExtraTemplates);
        else
           AnimationLibrary.OnFinalized(op, Setup_FinalPreparing);

        op.Begin();
    }



    //----------------------------------------------------------------------------------------------------- SETUP EXTRA TEMPLATES
    void Setup_ExtraTemplates()
    {
        //DISABLED: extra templates
        Setup_FinalPreparing();
        return;

        var opp = AnimationLibrary.Opacity(skipButton, to: 1, duration: 1);
        opp.Begin();


        setupStep = 3;   
        setupGrid_comfy.Visibility = Visibility.Collapsed;

        mainText_extraTemplates.Opacity = 0;
        mainText_extraTemplates.Visibility = Visibility.Visible;


        var op0 = AnimationLibrary.Opacity(mainText_extraTemplates, to: 1, duration: 1);
        op0.Begin();

        var mov = AnimationLibrary.Translate(mainText_extraTemplates, toX: 0, toY: 0, duration: 2, fromY: 20);
        mov.Begin();

        setupGrid_extraTemplates.Opacity = 0;
        setupGrid_extraTemplates.Visibility = Visibility.Visible;
        var op2 = AnimationLibrary.Opacity(setupGrid_extraTemplates, to: 1, duration: 1);
        op2.Begin();


        DoAction.Do(start, 0.5);
        sectionGrid.Opacity = 0;
        sectionGrid.Visibility = Visibility.Visible;
        void start()
        {
            sectionGrid.IsEnabled = true;
            var op = AnimationLibrary.Opacity(sectionGrid, to: 1, duration: 1);
            op.Begin();
            
        }

    }





    //----------------------------------------------------------------------------------------------------- SETUP FINAL PREPARING
    void Setup_infinalPreparing()
    {
        var op0 = AnimationLibrary.Opacity(sectionGrid, to: 0, duration: 1);
        AnimationLibrary.OnFinalized(op0, finish);
        op0.Begin();

        void finish()
        {
            sectionGrid.Visibility = Visibility.Collapsed;
            sectionGrid.IsEnabled = false;
        }

        var op2 = AnimationLibrary.Opacity(skipButton, to: 0, duration: 1);
        op2.Begin();


        var op = AnimationLibrary.Opacity(setupGrid_extraTemplates, to: 0, duration: 1);
        AnimationLibrary.OnFinalized(op, Setup_FinalPreparing);
        op.Begin();

   
    }


    void Setup_FinalPreparing()
    {
        mainText_extraTemplates.Opacity = 0;
        mainText_extraTemplates.Visibility = Visibility.Collapsed;

        setup_startManual.Opacity = 0;
        setup_startManual.Visibility = Visibility.Collapsed;

        setupStep = 4;

        sectionGrid.Visibility = Visibility.Collapsed;
        setupGrid_extraTemplates.Visibility = Visibility.Collapsed;

        setupGrid_finalPreparing.Opacity = 0;
        setupGrid_finalPreparing.Visibility = Visibility.Visible;
        setupGrid_extraTemplates.Visibility = Visibility.Visible;


        var op = AnimationLibrary.Opacity(setupGrid_finalPreparing, to: 1, duration: 1);
        if((!skipedComfy && !Settings.instance.AIServer.IsDownloaded) || AppModel.launcher.isDownloading)
            AnimationLibrary.OnFinalized(op, loaded);

        op.Begin();

        var opp = AnimationLibrary.Opacity(skipButton, to: 0, duration: 1);
        opp.Begin();

        //two maintexts
        var b = AnimationLibrary.BreatheOpacity(mainText_panelFinal);
        b.Begin();

        if ((!skipedComfy && !Settings.instance.AIServer.IsDownloaded) || AppModel.launcher.isDownloading)
        {
            var op8 = AnimationLibrary.Translate(mainText_panelFinal, toX: 0, toY: -20, duration: 2);
            op8.Begin();
        }

        void loaded()
        {
            mainText_finalPreparing2.Opacity = 0;
            //text2
            var op2 = AnimationLibrary.Opacity(mainText_finalPreparing2, to: 1, duration: 1);
            op2.Begin();



            //glow
            final_blinkGlow.Opacity = 0;
            ActionHistory.DoAction.Do(() =>
            {
                var op4 = AnimationLibrary.Opacity(final_blinkGlow, to: 1, duration: 1);
                AnimationLibrary.OnFinalized(op4, blink_off);
                op4.Begin();

                void blink_off()
                {
                    var op5 = AnimationLibrary.Opacity(final_blinkGlow, to: 0, duration: 4);
                    AnimationLibrary.OnFinalized(op5, end_text2);
                    op5.Begin();
                }

                void end_text2()
                {
                    DoAction.Do(() => { 
                    var op6 = AnimationLibrary.Opacity(mainText_finalPreparing2, to: 0, duration: 2);
                    op6.Begin();

                        var mov = AnimationLibrary.Translate(mainText_panelFinal, toX: 0, toY: 20, duration: 2);
                        mov.Begin();

                    }, 5);
                }

            }, 1);

        }


        if (skipedComfy || !AppModel.launcher.isDownloading)
            DoAction.Do(Setup_StartManual, 2);
   

    }



    //----------------------------------------------------------------------------- FINISHED, START MANUAL

    public void Setup_StartManual()
    {
        setupGrid_comfy.Visibility = Visibility.Collapsed;
        setupGrid_extraTemplates.Visibility = Visibility.Collapsed;
        setupGrid_signIn.Visibility = Visibility.Collapsed;
        setupGrid.Visibility = Visibility.Collapsed;
        TaskBar.Notification();

        setup_startManual.Opacity = 0;
        setup_startManual.Visibility = Visibility.Visible;


        var op0 = AnimationLibrary.Opacity(backButton, to: 0, duration: 4);
        op0.Begin();
        backButton.IsEnabled = false;

        var op = AnimationLibrary.Opacity(mainText_panelFinal, to: 0, duration: 2);
        op.OnFinalized(start);
        op.Begin();

        var mov = AnimationLibrary.Translate(mainText_panelFinal, toX: 0, toY: -20, duration: 2);
        mov.Begin();


        void start()
        {
            var op2 = AnimationLibrary.Opacity(setup_startManual, to: 1, duration: 4);
            op2.Begin();
        }

        var b = AnimationLibrary.BreatheOpacity(fin_btnLoad);
        b.Begin();

    }


    bool startingManual = false;
    private void Button_ClickStartManual(object sender, RoutedEventArgs e)
    {
        startingManual = true;
        var op = AnimationLibrary.Opacity(startManualBtn, to: 0, duration: 4);
        startManualBtn.IsEnabled = false;
        op.OnFinalized(start);
        op.Begin();

        void start()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            App.PostNormalStartup();
            window.Close();
            Mouse.OverrideCursor = null;
        }

    }






    //--------------------------- GENERAL BEHAVIOUR
    private void Button_Skip(object sender, RoutedEventArgs e)
    {
        Skip();
    }



    //--------------------------------------------------------------------- SKIP AND BACK
    public int setupStep = 0;
    void Skip()
    {
        
        if (setupStep == 1)
        {
            Setup_ComfyStart();
        }
        else if(setupStep == 2)
        {
            skipedComfy = true;
            Setup_comfyFinal();
        }
        else if (setupStep == 3)
        {
            Setup_infinalPreparing();
        }

    }


    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (setupStep == 2)
        {

            var op0 = AnimationLibrary.Opacity(backButton, to: 0, duration: 1);
            op0.Begin();
            backButton.IsEnabled = false;

            var mov = AnimationLibrary.Translate(setupGrid_comfy, toX: 20, toY: 0, duration: 1);
            mov.Begin();

            var op = AnimationLibrary.Opacity(setupGrid_comfy, to: 0, duration: 1);
            AnimationLibrary.OnFinalized(op, Setup_signIn);
            op.Begin();
        }
        else if (setupStep == 3 || (setupStep == 4 && skipedComfy))
        {
            var mov = AnimationLibrary.Translate(setupGrid_extraTemplates, toX: 20, toY: 0, duration: 1);
            mov.Begin();

            var op = AnimationLibrary.Opacity(setupGrid_extraTemplates, to: 0, duration: 1);
            AnimationLibrary.OnFinalized(op, Setup_ComfyStart);
            op.Begin();
        }
        else if (setupStep == 4)
        {
            var mov = AnimationLibrary.Translate(setupGrid_finalPreparing, toX: 20, toY: 0, duration: 1);
            mov.Begin();

            var op = AnimationLibrary.Opacity(setupGrid_finalPreparing, to: 0, duration: 1);
            //  AnimationLibrary.OnFinalized(op, Setup_ExtraTemplates);
            AnimationLibrary.OnFinalized(op, Setup_ComfyStart);
            op.Begin();
        }
    }
  

    
    //---------------------------------------------------------------------------------------------------------------------------------------

    public W_Launcher()
    {
        InitializeComponent();
        DataContext = AppModel.launcher;
        Loaded += W_AssetStore_Loaded;

        CommonStart();
    }


    void start()
    {
        TaskBar.SetWindow(window);
        AppModel.windowAssetStore = this.window;


        window.MinWidth = 600;
        window.MinHeight = 400;
    }

 

    public override void Window_Closed(object? sender, EventArgs e)
    {
        AppModel.windowAssetStore = null;
        base.Window_Closed(sender, e);

        if (isSetup && !startingManual)
            Application.Current.Shutdown();
    }

    private void downloadMenu_Click(object sender, RoutedEventArgs e)
    {
        // Button button = sender as Button;
        downloadPopup.IsOpen = !downloadPopup.IsOpen;

        var s = AnimationLibrary.Size(borderdownloadPopup, toX: borderdownloadPopup.Width, toY: borderdownloadPopup.Height, duration: 0.3, fromY: 1);
        borderdownloadPopup.Opacity = 0;
        s.Begin();

        var op = AnimationLibrary.Opacity(borderdownloadPopup, to: 1, duration: 0.3);
        op.Begin();
    }


    private void SetL_StoreTemplate()
    {
        // Crear el DataTemplate
        DataTemplate storeTemplate = new DataTemplate();

        // Utilizar FrameworkElementFactory para crear el contenido del DataTemplate
        FrameworkElementFactory factory = new FrameworkElementFactory(typeof(L_StoreView));

        // Asignar el FrameworkElementFactory al DataTemplate
        storeTemplate.VisualTree = factory;

        // Establecer el tipo de datos para el DataTemplate
        storeTemplate.DataType = typeof(L_Store);

        // Agregar el DataTemplate a los recursos de la ventana
        this.Resources.Add(new DataTemplateKey(typeof(L_Store)), storeTemplate);
    }

    private void Button_AICloud(object sender, RoutedEventArgs e)
    {
        WebManager.OPEN("https://manualai.art/contact");
    }

    private void Button_Click_Token(object sender, RoutedEventArgs e)
    {
        if (UserManager.LoginTokenClipboard())
             Setup_ComfyStart();
             
    }
}





