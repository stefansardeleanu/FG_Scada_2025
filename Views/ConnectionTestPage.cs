using FG_Scada_2025.ViewModels;
using System.Collections.ObjectModel;

namespace FG_Scada_2025.Views
{
    public partial class ConnectionTestPage : ContentPage
    {
        private readonly ConnectionTestViewModel _viewModel;
        private Entry _hostEntry;
        private Entry _portEntry;
        private Entry _usernameEntry;
        private Entry _passwordEntry;
        private Entry _topicEntry;
        private Label _statusLabel;
        private Label _messagesLabel;
        private Button _connectButton;
        private Button _disconnectButton;
        private Button _subscribeButton;

        public ConnectionTestPage(ConnectionTestViewModel viewModel)
        {
            _viewModel = viewModel;
            BindingContext = _viewModel;
            Title = "MQTT Connection Test";

            CreateUI();
            SetupBindings();
        }

        private void CreateUI()
        {
            var scrollView = new ScrollView();
            var mainStack = new StackLayout
            {
                Padding = new Thickness(20),
                Spacing = 15
            };

            // Header
            var headerLabel = new Label
            {
                Text = "MQTT Connection Test",
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainStack.Children.Add(headerLabel);

            // Connection Settings Frame
            var settingsFrame = CreateConnectionSettingsFrame();
            mainStack.Children.Add(settingsFrame);

            // Control Buttons
            var buttonsStack = CreateControlButtons();
            mainStack.Children.Add(buttonsStack);

            // Status Display
            var statusFrame = CreateStatusFrame();
            mainStack.Children.Add(statusFrame);

            // Topic Subscription
            var topicFrame = CreateTopicFrame();
            mainStack.Children.Add(topicFrame);

            // Messages Display
            var messagesFrame = CreateMessagesFrame();
            mainStack.Children.Add(messagesFrame);

            // Back Button
            var backButton = new Button
            {
                Text = "Back to Main",
                BackgroundColor = Color.FromArgb("#6c757d"),
                TextColor = Colors.White,
                HeightRequest = 45,
                CornerRadius = 8,
                Margin = new Thickness(0, 20, 0, 0)
            };
            backButton.SetBinding(Button.CommandProperty, "BackCommand");
            mainStack.Children.Add(backButton);

            scrollView.Content = mainStack;
            Content = scrollView;
        }

        private Frame CreateConnectionSettingsFrame()
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#f8f9fa"),
                Padding = new Thickness(15),
                CornerRadius = 8
            };

            var stack = new StackLayout { Spacing = 10 };

            var titleLabel = new Label
            {
                Text = "Connection Settings",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold
            };
            stack.Children.Add(titleLabel);

            var grid = new Grid
            {
                RowDefinitions = { new RowDefinition(), new RowDefinition(), new RowDefinition(), new RowDefinition() },
                ColumnDefinitions = { new ColumnDefinition { Width = 120 }, new ColumnDefinition { Width = GridLength.Star } },
                RowSpacing = 10,
                ColumnSpacing = 10
            };

            // Host
            grid.Add(new Label { Text = "Broker Host:", VerticalOptions = LayoutOptions.Center }, 0, 0);
            _hostEntry = new Entry { Placeholder = "e.g., atsdhala2.ddns.net" };
            _hostEntry.SetBinding(Entry.TextProperty, "BrokerHost");
            grid.Add(_hostEntry, 1, 0);

            // Port
            grid.Add(new Label { Text = "Port:", VerticalOptions = LayoutOptions.Center }, 0, 1);
            _portEntry = new Entry { Placeholder = "1883", Keyboard = Keyboard.Numeric };
            _portEntry.SetBinding(Entry.TextProperty, "BrokerPort");
            grid.Add(_portEntry, 1, 1);

            // Username
            grid.Add(new Label { Text = "Username:", VerticalOptions = LayoutOptions.Center }, 0, 2);
            _usernameEntry = new Entry { Placeholder = "Optional" };
            _usernameEntry.SetBinding(Entry.TextProperty, "Username");
            grid.Add(_usernameEntry, 1, 2);

            // Password
            grid.Add(new Label { Text = "Password:", VerticalOptions = LayoutOptions.Center }, 0, 3);
            _passwordEntry = new Entry { Placeholder = "Optional", IsPassword = true };
            _passwordEntry.SetBinding(Entry.TextProperty, "Password");
            grid.Add(_passwordEntry, 1, 3);

            stack.Children.Add(grid);
            frame.Content = stack;
            return frame;
        }

        private StackLayout CreateControlButtons()
        {
            var stack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 15
            };

            _connectButton = new Button
            {
                Text = "Connect",
                BackgroundColor = Color.FromArgb("#28a745"),
                TextColor = Colors.White,
                WidthRequest = 100,
                HeightRequest = 45,
                CornerRadius = 8
            };
            _connectButton.SetBinding(Button.CommandProperty, "ConnectCommand");

            _disconnectButton = new Button
            {
                Text = "Disconnect",
                BackgroundColor = Color.FromArgb("#dc3545"),
                TextColor = Colors.White,
                WidthRequest = 100,
                HeightRequest = 45,
                CornerRadius = 8
            };
            _disconnectButton.SetBinding(Button.CommandProperty, "DisconnectCommand");

            stack.Children.Add(_connectButton);
            stack.Children.Add(_disconnectButton);

            return stack;
        }

        private Frame CreateStatusFrame()
        {
            var frame = new Frame
            {
                Padding = new Thickness(15),
                CornerRadius = 8
            };
            frame.SetBinding(BackgroundColorProperty, "StatusBackgroundColor");

            var stack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 10
            };

            var statusIndicator = new BoxView
            {
                WidthRequest = 20,
                HeightRequest = 20,
                CornerRadius = 10
            };
            statusIndicator.SetBinding(BoxView.BackgroundColorProperty, "StatusColor");

            _statusLabel = new Label
            {
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };
            _statusLabel.SetBinding(Label.TextProperty, "ConnectionStatus");

            stack.Children.Add(statusIndicator);
            stack.Children.Add(_statusLabel);

            frame.Content = stack;
            return frame;
        }

        private Frame CreateTopicFrame()
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#e9ecef"),
                Padding = new Thickness(15),
                CornerRadius = 8
            };

            var stack = new StackLayout { Spacing = 10 };

            var titleLabel = new Label
            {
                Text = "Test Topic Subscription",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold
            };
            stack.Children.Add(titleLabel);

            var grid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = 100 } },
                ColumnSpacing = 10
            };

            _topicEntry = new Entry { Placeholder = "e.g., PLCNext/+" };
            _topicEntry.SetBinding(Entry.TextProperty, "TestTopic");
            grid.Add(_topicEntry, 0, 0);

            _subscribeButton = new Button
            {
                Text = "Subscribe",
                BackgroundColor = Color.FromArgb("#007bff"),
                TextColor = Colors.White,
                HeightRequest = 40,
                CornerRadius = 5
            };
            _subscribeButton.SetBinding(Button.CommandProperty, "SubscribeCommand");
            grid.Add(_subscribeButton, 1, 0);

            stack.Children.Add(grid);
            frame.Content = stack;
            return frame;
        }

        private Frame CreateMessagesFrame()
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#f8f9fa"),
                Padding = new Thickness(15),
                CornerRadius = 8
            };

            var stack = new StackLayout { Spacing = 10 };

            var titleLabel = new Label
            {
                Text = "Received Messages",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold
            };
            stack.Children.Add(titleLabel);

            _messagesLabel = new Label
            {
                Text = "No messages received yet...",
                FontSize = 12,
                TextColor = Colors.Gray
            };
            stack.Children.Add(_messagesLabel);

            frame.Content = stack;
            return frame;
        }

        private void SetupBindings()
        {
            // Subscribe to property changes to update messages display
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.ReceivedMessages))
                {
                    UpdateMessagesDisplay();
                }
            };

            // Subscribe to collection changes
            _viewModel.ReceivedMessages.CollectionChanged += (s, e) =>
            {
                UpdateMessagesDisplay();
            };
        }

        private void UpdateMessagesDisplay()
        {
            if (_viewModel.ReceivedMessages.Count == 0)
            {
                _messagesLabel.Text = "No messages received yet...";
                _messagesLabel.TextColor = Colors.Gray;
            }
            else
            {
                var recentMessages = _viewModel.ReceivedMessages.Take(5);
                var displayText = string.Join("\n\n", recentMessages.Select(m =>
                    $"[{m.Timestamp:HH:mm:ss}] {m.Topic}\n{m.Payload}"));

                _messagesLabel.Text = displayText;
                _messagesLabel.TextColor = Colors.Black;
            }
        }
    }
}