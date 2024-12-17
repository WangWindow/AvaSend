using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Reactive.Linq;
using AvaSend.ViewModels;
using ReactiveUI;

namespace AvaSend.Views
{
    public partial class ReceiveView : UserControl
    {
        private readonly RotateTransform _rotateTransform;
        private readonly DispatcherTimer _timer;

        public ReceiveView()
        {
            InitializeComponent();
            _rotateTransform = new RotateTransform();
            var iconImage = this.FindControl<Image>("IconImage");
            iconImage.RenderTransform = _rotateTransform;

            var viewModel = (ReceiveViewModel)DataContext;
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

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _timer.Tick += (sender, e) =>
            {
                _rotateTransform.Angle += 1;
                if (_rotateTransform.Angle >= 360)
                {
                    _rotateTransform.Angle = 0;
                }
            };
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
}
