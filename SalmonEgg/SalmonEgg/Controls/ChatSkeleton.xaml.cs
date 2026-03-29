using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SalmonEgg.Controls
{
    public sealed partial class ChatSkeleton : UserControl
    {
        public ChatSkeleton()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) => ShimmerStoryboard.Begin();
        }
    }
}
