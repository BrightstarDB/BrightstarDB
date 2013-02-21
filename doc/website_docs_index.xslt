<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
    <xsl:output method="html" indent="yes" omit-xml-declaration="yes" />

	<xsl:param name="docmap" select="document('docmap.xml')"/>
  <xsl:template match="map">
    <xsl:text disable-output-escaping="yes"><![CDATA[<!DOCTYPE html>]]></xsl:text>
    <html>
      <head>
        <title>BrightstarDB - Documentation</title>
        <link rel="stylesheet" href="/css/reset.css"/>
        <link rel="stylesheet" href="/css/main.css"/>
        <link rel="icon" href="/images/favicon.ico" />
        <link rel="alternate" type="application/rss+xml" title="Brightstar Blog Feed" href="/blog/?feed=rss2" />

        <script type="text/javascript" src="https://ajax.googleapis.com/ajax/libs/jquery/1.6.2/jquery.min.js"></script>
        <script type="text/javascript" src="/js/modernizr-1.7.min.js"></script>

        <style>
          dt {
            margin-top: 1em;
            font-size: 24px;
            font-weight: bold;
          }
          dd{
            margin-left: 2em;
            margin-bottom: 1em;
          }
          dt a {
            text-decoration: none;
          }
          dt a:hover {
            text-decoration : underline;
          }
        </style>
      </head>
      <body>
		<xsl:comment>#include file="/header.inc"</xsl:comment>
		<!--#include file="header.inc"-->
        <article class="documentation">
          <header>
            <h1>Index</h1>
          </header>
          <dl>
            <xsl:apply-templates/>
          </dl>
        </article>
        <xsl:comment>#include file="/footer.inc"</xsl:comment>
        
        <!-- Place this tag in your head or just before your close body tag -->
        <script type="text/javascript" src="https://apis.google.com/js/plusone.js">
          {lang: 'en-GB'}
        </script>
        <!-- google analytics -->
        <script type="text/javascript">
            var _gaq = _gaq || [];
            _gaq.push(['_setAccount', 'UA-25503555-1']);
            _gaq.push(['_trackPageview']);

            (function () {
                var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
                ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
                var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
            })();
        </script>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="topicref">
	<xsl:variable name="href" select="@href"/>
    <dt>
      <a href="{@href}.html">
        <xsl:value-of select="caption"/>
      </a>
    </dt>
    <dd>
      <xsl:apply-templates select="$docmap/docmap/doc[@href=$href]/description"/>
    </dd>
  </xsl:template>

  <xsl:template match="description">
    <xsl:copy-of select="* | text()"/>
  </xsl:template>
  
    <xsl:template match="@* | node()">
        <xsl:copy>
            <xsl:apply-templates select="@* | node()"/>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>