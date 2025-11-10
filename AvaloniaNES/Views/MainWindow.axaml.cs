using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaNES.Device.BUS;
using AvaloniaNES.Models;
using AvaloniaNES.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AvaloniaNES.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _renderTimer;
    private readonly NESStatus _status = App.Services.GetRequiredService<NESStatus>();
    private readonly Bus _bus = App.Services.GetRequiredService<Bus>();

    private readonly Stopwatch _delayWatch = new();
    private readonly Stopwatch _cycleWatch = new();

    private double _ppuCycle = 16.66;

    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;

        m_Video.Source = _bus.PPU!.GetScreen();
        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // 60FPS
        };
        _renderTimer.Tick += OnRenderFrame;
        _renderTimer.Start();  //Complete 1 Frame is 16.666666ms

        Task.Run(() =>
        {
            while (true)
            {
                _cycleWatch.Restart();
                if (_status.HasLoadRom && _status.BusState == BUS_STATE.RUN)
                {
                    do
                    {
                        _bus.Clock();
                    } while (!_bus.PPU!.FrameCompleted);
                    _bus.PPU!.FrameCompleted = false;
                }
                _cycleWatch.Stop();
                //Delay
                delayMs(_ppuCycle - _cycleWatch.ElapsedMilliseconds);
            }
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Environment.Exit(0);
    }

    private void OnRenderFrame(object? sender, EventArgs e)
    {
        if (_status.HasLoadRom)
        {
            m_Video.InvalidateMeasure();
            m_Video.InvalidateArrange();
            m_Video.InvalidateVisual();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.HandleKeyDown(e.Key);
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.HandleKeyUp(e.Key);
        }
    }

    private void delayMs(double time)
    {
        _delayWatch.Restart();
        while (_delayWatch.Elapsed.TotalMilliseconds < time) { }
        _delayWatch.Stop();
    }

    //speed
    private void MenuItem_x1_OnClick(object? sender, RoutedEventArgs e)
    {
        _ppuCycle = 16.66;
    }

    private void MenuItem_x2_OnClick(object? sender, RoutedEventArgs e)
    {
        _ppuCycle = 8.33;
    }

    private void MenuItem_x4_OnClick(object? sender, RoutedEventArgs e)
    {
        _ppuCycle = 4.16;
    }
}