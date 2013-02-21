<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="website.installation._default" %>

<!doctype html>
<html>
<head>
    <meta charset="utf-8" />
    <title>BrightstarDB - NoSQL for .NET</title>
    <link rel="stylesheet" href="/css/reset.css" />
    <link rel="Stylesheet" href="/css/main.css" />
    <link rel="icon" href="/images/favicon.ico" />
    <script type="text/javascript" src="https://ajax.googleapis.com/ajax/libs/jquery/1.6.2/jquery.min.js"></script>
    <script type="text/javascript" src="/js/modernizr-1.7.min.js"></script>
</head>
<body>
    <!--#include file="/header.inc"-->
    <%--
    <header id="header">
        <img class="logo" src="/images/logo.png" alt="BrightstarDB" />
    </header>
    <nav id="sitenav">
        <a href="/documentation/">Documentation</a>
        <a href="/download/">Download</a>
        <a href="/service/">Service</a>
        <a href="/ecosystem/">Ecosystem</a>
        <a href="/blog/">Blog</a>
    </nav>
    --%>
    <%if (NewInstallation && CurrentVersion)
      {%>
    <!-- Installed the current version, provide links -->
    <article class="post">
        <header>
            <hgroup>
                <h1 class="logotext">
                    Thanks for installing Brightstar<span class="logotext-highlight">DB</span></h1>
            </hgroup>
        </header>
        <p>
            We hope that you find BrightstarDB the fastest, most pain-free way to develop data-driven
            .NET applications. To help you get going, we have created a number of sample applications
            and resources that walk you through building your first BrightstarDB applications.
            You can find them in our <a href="../documentation/">online documentation</a>.
        </p>
        <p>
            To develop with BrightstarDB or to run a BrightstarDB server, you will require your
            license key. If you have purchased a license already, you can <a href="/customers">claim and view your license keys here</a>.
            If you have not yet purchased a license and wish to evaluate BrightstarDB, you must <a href="/customers">register to claim your
            free trial license here</a>.
        </p>
        <p>
            We have also created a <a href="../community/">user forum</a> where you can post
            your questions and get answers from the BrightstarDB developers and our community
            of users.</p>
    </article>
    <% }
      else if (Upgrade && CurrentVersion)
      {%>
    <!-- Upgraded to current version -->
    <article class="post">
        <header>
            <hgroup>
                <h1 class="logotext">
                    You are now on the latest version of Brightstar<span class="logotext-highlight">DB</span></h1>
            </hgroup>
        </header>
        <p>
            Thanks for taking the time to upgrade to the latest version of BrightstarDB. If
            you experienced any difficulties during the upgrade process or if you have any questions
            relating to BrightstarDB, please let us know via our <a href="../community/">user forum.</a></p>
        <p><strong>PLEASE NOTE:</strong> If you are upgrading from a previous MAJOR version of BrightstarDB,
            you will require a new license key to use the software. Your new license key should already be
            accessible from <a href="/customers">your account</a>. If you are having trouble finding your
            new license key please <a href="mailto:support@brightstardb.com">contact us</a>.
        </p>
    </article>
    <% }
      else if ((NewInstallation || Upgrade) && !CurrentVersion)
      { %>
    <!-- Installed or upgraded to an old version -->
    <article class="post">
        <header>
            <hgroup>
                <h1 class="logotext">
                    Hold on a minute!</h1>
            </hgroup>
        </header>
        <p>
            <strong>Did you know that there is a newer version of BrightstarDB available?</strong></p>
        <p>
            Please take a few minutes of your time to <a href="../download/">visit our Download
                page</a> to get the latest installer and run that to upgrade your BrightstarDB.</p>
    </article>
    <%}
      else if (Uninstalled)
      {%>
    <!-- Uninstall feedback form -->
    <article class="post">
        <header>
            <hgroup>
                <h1 class="logotext">
                    Sorry to see you go...</h1>
            </hgroup>
        </header>
        <p>
            Thanks for trying out BrightstarDB. We are sorry that you are leaving us.</p>
        <p>
            To help us do better in future, could you please fill out the short questionnaire
            below. All feedback is helpful to us as we try to make BrightstarDB the most pain-free
            way to develop data-driven .NET applications.</p>
        <iframe src="https://docs.google.com/a/networkedplanet.com/spreadsheet/embeddedform?formkey=dFBYSHAxa1lnLUQ2ZTNVeS1zSmRyR2c6MQ"
            width="760" height="1065" frameborder="0" marginheight="0" marginwidth="0">Loading...</iframe>
    </article>
    <% }
      else
      { %>
    <!-- Default catch-all for something we didn't handle -->
    <article class="post">
        <header>
            <hgroup>
                <h1 class="logotext">
                    Installation Complete</h1>
            </hgroup>
        </header>
        <p>
            We hope that you find BrightstarDB the fastest, most pain-free way to develop data-driven
            .NET applications. To help you get going, we have created a number of sample applications
            and resources that walk you through building your first BrightstarDB applications.
            You can find them in our <a href="../documentation/">online documentation</a>.</p>
        <p>
            We have also created a <a href="../community/">user forum</a> where you can post
            your questions and get answers from the BrightstarDB developers and our community
            of users.</p>
    </article>
    <% } %>
    <!--#include file="/footer.inc"-->
        <%--
        <footer id="footer">
        <nav>
            <h1>
                Navigation</h1>
            <ul>
                <li><a href="/documentation/">Documentation</a></li>
                <li><a href="/download/">Download</a></li>
                <li><a href="/service/">Service</a></li>
                <li><a href="/ecosystem/">Ecosystem</a></li>
                <li><a href="/blog/">Blog</a></li>
                <li><a href="/community/">Community</a></li>
                <li><a href="/about/">About / Contact</a></li>
            </ul>
        </nav>
        <nav>
            <h1>
                Keep in Touch!</h1>
            <p>
                <a href="http://twitter.com/brightstardb" class="twitter-follow-button" data-show-count="false">
                    Follow @BrightstarDB</a></p>
            <script src="http://platform.twitter.com/widgets.js" type="text/javascript"></script>
            <div data-href="http://brightstardb.com/" class="g-plusone" data-size="medium">
            </div>
            <p>
                <img src="/images/feed-icon16x16.png" alt="RSS Feed Icon" style="vertical-align: middle" />
                <a href="/blog/?feed=rss2">Blog RSS Feed</a>
            </p>
        </nav>
        <p>
            &copy; BrightstarDB Limited 2012</p>
        <p>
            Registered in England and Wales no. 7992038. Registered Office: 17 Cosgrove Road,
            Old Stratford, Milton Keynes, MK19 6AG</p>
    </footer>
        --%>
</body>
</html>
