using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Reactive.Linq;
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

        // ʹ�� TransformGroup ��� RotateTransform �� ScaleTransform
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_rotateTransform);
        transformGroup.Children.Add(_scaleTransform);
        iconImage.RenderTransform = transformGroup;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100) // ��������Լ�����ת�ٶ�
        };
        _timer.Tick += (sender, e) =>
        {
            _rotateTransform.Angle += 1;
            if (_rotateTransform.Angle >= 360)
            {
                _rotateTransform.Angle = 0;
            }

            // ���Ŷ����߼�
            _scaleTransform.ScaleX += 0.01 * _scaleDirection;
            _scaleTransform.ScaleY += 0.01 * _scaleDirection;
            if (_scaleTransform.ScaleX >= 1.2 || _scaleTransform.ScaleX <= 0.8)
            {
                _scaleDirection *= -1;
            }
        };

        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is ReceiveViewModel viewModel)
        {
            viewModel.WhenAnyValue(vm => vm.IsAnimationEnabled)
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
