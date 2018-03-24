<%-- 
<copyright file="Default.aspx" company="Personal">
Copyright (c) 2013 All Rights Reserved
</copyright>
<author>Mario Szpuszta</author>
<date>2013-8-7, 10:44</date>
<summary>This is a sample and demo - use it at your full own risk!</summary>
--%>
<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ThumbnailFrontend._Default" %>

<asp:Content runat="server" ID="FeaturedContent" ContentPlaceHolderID="FeaturedContent">
    <section class="featured">
        <div class="content-wrapper">
            <hgroup class="title">
                <h1><%: Title %>Upload images and start thumbnail production</h1>
            </hgroup>
            <p>
                Here you can kick of the processing of creating thumbnails from images you have stored under a publicly reachable URL. 
                Just enter the URL in the textbox below and hit the button 'Start Thumbnail Creation' and an asychronous worker will
                use a legacy console app tool (the core of the demo:)) to create thumbnails. The idea of this demo is to show, how a console
                legacy app can be used without modification in a WorkerRole as a command line tool to get the full benefits from PaaS as 
                long as the console app is "self-contained" in a sense it gets all it needs through external interfaces (e.g. command line
                arguments or if its a WCF self-hosted service through the service interface etc.).
            </p>
        </div>
    </section>
</asp:Content>
<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    Specify the image to be processed through its full URL:
    <br />
    <asp:TextBox ID="SourceImageUrlText" runat="server" Width="90%"></asp:TextBox>
    <br />
    Now specify the target name of your image:<br />
    <asp:TextBox ID="TargetImageNameText" runat="server" Width="90%"></asp:TextBox>
    <br />
    <asp:Button ID="StartProcessingButton" runat="server" Text="Start processing the image" OnClick="StartProcessingButton_Click" />
    <br />
    <asp:Label ID="StatusLabel" runat="server" Text="Label"></asp:Label>
    </asp:Content>
