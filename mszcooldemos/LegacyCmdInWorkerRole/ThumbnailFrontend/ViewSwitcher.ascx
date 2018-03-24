<%-- 
<copyright file="ViewSwitcher.ascx" company="Personal">
Copyright (c) 2013 All Rights Reserved
</copyright>
<author>Mario Szpuszta</author>
<date>2013-8-7, 10:44</date>
<summary>This is a sample and demo - use it at your full own risk!</summary>
--%>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ViewSwitcher.ascx.cs" Inherits="ThumbnailFrontend.ViewSwitcher" %>
<div id="viewSwitcher">
    <%: CurrentView %> view | <a href="<%: SwitchUrl %>">Switch to <%: AlternateView %></a>
</div>