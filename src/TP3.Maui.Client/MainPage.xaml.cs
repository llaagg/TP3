using Microsoft.Maui.Controls;
using System;

namespace TP3.Maui.Client;

public partial class MainPage : ContentPage
{
    private readonly AgentNode _agent;

    public MainPage(AgentNode agent)
    {
        InitializeComponent();
        _agent = agent;
    }

    private void OnSendClicked(object sender, EventArgs e)
    {
        _agent.SendCommand("Ping", new { Time = DateTime.UtcNow });
        StatusLabel.Text = "Status: sent Ping";
    }
}
