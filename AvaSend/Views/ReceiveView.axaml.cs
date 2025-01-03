using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using AvaSend.ViewModels;
using ReactiveUI;

namespace AvaSend.Views;

public partial class ReceiveView : UserControl
{
    private readonly RotateTransform _rotateTransform;
    private readonly ScaleTransform _scaleTransform;
    private readonly DispatcherTimer _timer;
    private double _scaleDirection = 1;

    public ReceiveView()
    {
        InitializeComponent();
        _rotateTransform = new RotateTransform();
        _scaleTransform = new ScaleTransform();
        var iconImage = this.FindControl<Image>("IconImage");

        // 使用 TransformGroup 组合 RotateTransform 和 ScaleTransform
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_rotateTransform);
        transformGroup.Children.Add(_scaleTransform);
        iconImage.RenderTransform = transformGroup;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100), // 调整间隔以减慢旋转速度
        };
        _timer.Tick += (sender, e) =>
        {
            _rotateTransform.Angle += 2;
            if (_rotateTransform.Angle >= 360)
            {
                _rotateTransform.Angle = 0;
            }

            // 缩放动画逻辑
            _scaleTransform.ScaleX += 0.01 * _scaleDirection;
            _scaleTransform.ScaleY += 0.01 * _scaleDirection;
            if (_scaleTransform.ScaleX >= 1.1 || _scaleTransform.ScaleX <= 0.9)
            {
                _scaleDirection *= -1;
            }
        };

        // 如果 DataContext 已经设置，直接调用 OnDataContextChanged 方法
        if (DataContext != null)
        {
            OnDataContextChanged(this, EventArgs.Empty);
        }

        // 订阅 DataContextChanged 事件
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is ReceiveViewModel viewModel)
        {
            viewModel
                .WhenAnyValue(vm => vm.IsAnimationEnabled)
                .Subscribe(isEnabled =>
                {
                    if (isEnabled)
                    {
                        StartAnimation();
                    }
                    else
                    {
                        StopAnimation();
                    }
                });

            // 检查初始状态
            if (viewModel.IsAnimationEnabled)
            {
                StartAnimation();
            }
            else
            {
                StopAnimation();
            }
        }
    }

    private void StartAnimation()
    {
        _timer.Start();
    }

    private void StopAnimation()
    {
        _timer.Stop();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
