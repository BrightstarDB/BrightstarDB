<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
  <xsl:output method="html" indent="yes" omit-xml-declaration="yes" encoding="utf-8"/>
  <xsl:param name="docname"></xsl:param>
  <xsl:param name="topicpath">src</xsl:param>
  <xsl:template match="map">
    <xsl:text disable-output-escaping="yes"><![CDATA[<!DOCTYPE html>]]></xsl:text>
    <xsl:variable name="self" select="current()"/>
    <xsl:variable name="docpath"><xsl:value-of select="$topicpath"/>/Topics/<xsl:value-of select="$docname"/>.xml</xsl:variable>
    <xsl:variable name="topic" select="document($docpath)"/>
    <html>
      <head>
        <meta charset="UTF-8"/>
        <title>
          Brightstar Documentation - <xsl:value-of select="topicref[@href=$docname]/caption"/>
        </title>
        <xsl:variable name="keywords">
          <xsl:apply-templates select="." mode="keywords"/>
        </xsl:variable>
        <meta name="keywords">
          <xsl:attribute name="content">
            <xsl:value-of select="normalize-space($keywords)"/>
          </xsl:attribute>
        </meta>
        <link rel="stylesheet" href="/css/reset.css"/>
        <link rel="stylesheet" href="/css/main.css"/>
        <link rel="stylesheet" href="/css/vs.min.css"/>
        <link rel="shortcut icon" href="/images/favicon.ico" />
        <script src="/js/jquery-1.5.1.min.js" type="text/javascript"></script>
        <script src="/js/modernizr-1.7.min.js" type="text/javascript"></script>
        <script src="/js/highlight.min.js" type="text/javascript"></script>
        <script type="text/javascript">
          hljs.tabReplace = '    ';
          hljs.initHighlightingOnLoad();
        </script>
        <script type="text/javascript">
          $(document).ready(function() {
            var $toc = $('.toc');
            var top = $toc.offset().top - parseFloat($toc.css('marginTop').replace(/auto/, 0));
            var left = $toc.offset().left - parseFloat($toc.css('marginLeft').replace(/auto/, 0));
            var viewWidth = $(document).width();
            var cssLeft = $toc.position().left;

            $(window).scroll(setFixedClass);

            function setFixedClass() {
              var y = $(this).scrollTop();
              if (y >= top) {
                $toc.addClass('fixed');
                $toc.css('left', left);
              } else {
                $toc.removeClass('fixed');
                $toc.css('left', cssLeft);
              }
            }

            var rtime = new Date(1, 1, 2000, 12,00,00);
            var timeout = false;
            var delta = 200;
            $(window).resize(function() {
                rtime = new Date();
                if (timeout === false) {
                  timeout = true;
                  setTimeout(resizeend, delta);
                }
            });

            function resizeend() {
               if (new Date() - rtime &lt; delta) {
                 setTimeout(resizeend, delta);
               } else {
                 timeout = false;
                 var currentWidth = $(document).width();
                 var difference = currentWidth - viewWidth;
                 viewWidth = currentWidth;
                 left = left + (difference / 2);
                 setFixedClass();
               }
            }
          });
        </script>
      </head>
      
      <body>
        <xsl:comment>#include file="/header.inc"</xsl:comment>

        <article class="documentation">
          <header>
            <h1>
              <xsl:value-of select="topicref[@href=$docname]/caption"/>
            </h1>
          </header>
          <nav class="toc">
            <header>
              <h1>Contents</h1>
            </header>
            <ul>
              <xsl:for-each select="topicref">
                <li>
                  <xsl:choose>
                    <xsl:when test="@href=$docname">
                      <strong>
                        <xsl:value-of select="caption"/>
                      </strong>
                      <ul>
                        <xsl:apply-templates select="topicref" mode="toc"/>
                      </ul>
                    </xsl:when>
                    <xsl:otherwise>
                      <a>
                        <xsl:attribute name="href">
                          <xsl:value-of select="@href"/>.html
                        </xsl:attribute>
                        <xsl:value-of select="caption"/>
                      </a>
                    </xsl:otherwise>
                  </xsl:choose>
                </li>
              </xsl:for-each>
            </ul>
          </nav>
          <xsl:apply-templates select="topicref[@href=$docname]" mode="no_heading" />
        </article>
		<xsl:comment>#include file="/footer.inc"</xsl:comment>

      </body>
    </html>
  </xsl:template>

  <xsl:template match="topicref" mode="keywords">
    <xsl:variable name="topicxml"><xsl:value-of select="$topicpath"/>/Topics/<xsl:value-of select="@href"/>.xml</xsl:variable>
    <xsl:apply-templates select="document($topicxml)/topic/keywords/keyword" mode="keywords"/>
  </xsl:template>

  <xsl:template match="keyword" mode="keywords">
    <xsl:apply-templates/>
    <xsl:text>,</xsl:text>
  </xsl:template>

  <xsl:template match="map" mode="toc">
    <ul>
      <xsl:apply-templates select="topicref" mode="toc"/>
    </ul>
  </xsl:template>

  <xsl:template match="topicref" mode="toc">
    <li>
      <a href="#{@href}">
        <xsl:value-of select="caption"/>
      </a>
      <xsl:if test="topicref">
        <ul>
          <xsl:for-each select="topicref">
            <li>
              <a href="#{@href}" style="font-size: small">
                <xsl:value-of select="caption"/>
              </a>
            </li>
          </xsl:for-each>
        </ul>
      </xsl:if>
    </li>
  </xsl:template>

  <xsl:template match="topicref" mode="no_heading">
    <xsl:variable name="topicxml">
      <xsl:value-of select="$topicpath"/>/Topics/<xsl:value-of select="@href"/>.xml
    </xsl:variable>
    <section>
      <xsl:attribute name="id">
        <xsl:value-of select="@href"/>
      </xsl:attribute>
      <xsl:apply-templates select="document($topicxml)" mode="no_heading"/>
      <xsl:apply-templates/>
    </section>
  </xsl:template>
  
  <xsl:template match="topicref">
    <xsl:variable name="topicxml"><xsl:value-of select="$topicpath"/>/Topics/<xsl:value-of select="@href"/>.xml</xsl:variable>
    <section>
      <xsl:attribute name="id">
        <xsl:value-of select="@href"/>
      </xsl:attribute>
      <xsl:apply-templates select="document($topicxml)"/>
      <xsl:apply-templates/>
    </section>
  </xsl:template>

  <xsl:template match="topic" mode="no_heading">
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="topic">
    <header>
      <h1>
        <xsl:value-of select="title"/>
      </h1>
    </header>
    <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template match="topicref/caption">
    <xsl:comment>
      End of section '<xsl:value-of select="text()"/>'
    </xsl:comment>
  </xsl:template>
  
  <xsl:template match="title">
    <!-- ignore content -->
  </xsl:template>

  <xsl:template match="header">
    <!-- Ignore content -->
  </xsl:template>

  <xsl:template match="para[table]" priority="1">
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="table">
    <table style="{@style}" border="1">
      <xsl:apply-templates select="../following-sibling::para[@styleclass='Image Caption']" mode="caption"/>
      <xsl:apply-templates/>
    </table>
  </xsl:template>

  <xsl:template match="tr | td | th">
    <xsl:copy>
      <xsl:copy-of select="@style"/>
      <xsl:apply-templates/>
    </xsl:copy>
  </xsl:template>
  <xsl:template match="para[@styleclass='Normal']">
    <xsl:if test="*">
      <p>
        <xsl:apply-templates/>
      </p>
    </xsl:if>
  </xsl:template>

  <xsl:template match="para[@styleclass='Heading1']">
    <h2>
      <xsl:apply-templates/>
    </h2>
  </xsl:template>

  <xsl:template match="*[@styleclass='Notes' and not(preceding-sibling::*[1]/@styleclass='Notes')
		       and not (../@styleclass='Notes')]">
    <div class="note">
      <xsl:call-template name="divcontent">
        <xsl:with-param name="content" select="."/>
      </xsl:call-template>
    </div>
  </xsl:template>

  <xsl:template match="*[@styleclass='Notes' and (preceding-sibling::*[1]/@styleclass='Notes')]" priority="1">
    <!--ignore-->
  </xsl:template>

  <xsl:template match="*[@styleclass='Warning' and not(preceding-sibling::*[1]/@styleclass='Warning')
		       and not (../@styleclass='Notes')]">
    <div class="warning">
      <xsl:call-template name="divcontent">
        <xsl:with-param name="content" select="."/>
      </xsl:call-template>
    </div>
  </xsl:template>

  <xsl:template match="*[@styleclass='Warning' and (preceding-sibling::*[1]/@styleclass='Warning')]" priority="1">
    <!--ignore-->
  </xsl:template>

  <xsl:template name="divcontent">
    <xsl:param name="content" />
    <xsl:apply-templates select="$content" mode="divcontent"/>
    <xsl:if test="$content/following-sibling::*[1]/@styleclass = $content/@styleclass">
      <xsl:call-template name="divcontent">
        <xsl:with-param name="content" select="$content/following-sibling::*[1]"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template match="para" mode="divcontent">
    <p>
      <xsl:apply-templates mode="divcontent"/>
    </p>
  </xsl:template>

  <xsl:template match="list[@type='ul']" mode="divcontent">
    <ul>
      <xsl:apply-templates mode="divcontent"/>
    </ul>
  </xsl:template>

  <xsl:template match="list[@type='ol']" mode="divcontent">
    <ol style="list-style-type:{@listtype}">
      <xsl:apply-templates mode="divcontent"/>
    </ol>
  </xsl:template>

  <xsl:template match="li" mode="divcontent">
    <li>
      <xsl:apply-templates/>
    </li>
  </xsl:template>

  <xsl:template match="para[@styleclass='Code Example' and not(preceding-sibling::para[1]/@styleclass='Code Example')]">
    <pre class="cs">
      <code>
      <xsl:call-template name="writecode">
        <xsl:with-param name="content" select="."/>
      </xsl:call-template>
      </code>
    </pre>
  </xsl:template>

  <xsl:template match="para[@styleclass='XML HTML Example' and not(preceding-sibling::para[1]/@styleclass='XML HTML Example')]">
    <pre class="xml">
      <code>
      <xsl:call-template name="writecode">
        <xsl:with-param name="content" select="."/>
      </xsl:call-template>
      </code>
    </pre>
  </xsl:template>

  <xsl:template name="writecode">
    <xsl:param name="content"/>
    <xsl:apply-templates select="$content/*"/>
    <xsl:if test="$content/following-sibling::*[1]/@styleclass = $content/@styleclass">
      <xsl:text>&#13;</xsl:text>
      <xsl:call-template name="writecode">
        <xsl:with-param name="content" select="$content/following-sibling::*[1]"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template match="para[@styleclass='Code Example' and (preceding-sibling::para[1]/@styleclass='Code Example')]">
    <!-- ignore -->
  </xsl:template>
  
  <xsl:template match="para[@styleclass='XML HTML Example' and (preceding-sibling::para[1]/@styleclass='XML HTML Example')]">
    <!-- ignore -->
  </xsl:template>

  <xsl:template match="para[@styleclass='Image Caption']">
    <!-- ignore -->
  </xsl:template>
  <xsl:template match="para" mode="caption">
    <caption>
      <xsl:apply-templates/>
    </caption>
  </xsl:template>

  <xsl:template match="list[@type='ul']">
    <ul>
      <xsl:apply-templates/>
    </ul>
  </xsl:template>

  <xsl:template match="list[@type='ol']">
    <ol style="list-style-type:{@listtype}">
      <xsl:apply-templates/>
    </ol>
  </xsl:template>

  <xsl:template match="li">
    <li>
      <xsl:apply-templates/>
    </li>
  </xsl:template>

  <xsl:template match="link[@type='weblink']">
	<a href="{@href}">
		<xsl:apply-templates/>
	</a>
  </xsl:template>

  <xsl:template match="link[@type='topiclink']">
	<xsl:variable name="topicref" select="@href"/>
	<xsl:variable name="docpath"><xsl:value-of select="$topicpath"/>/Maps/table_of_contents.xml</xsl:variable>
	<xsl:variable name="map" select="document($docpath)"/>
	<xsl:if test="$map/map"><xsl:message>Got the map</xsl:message></xsl:if>
	<xsl:message>Link to <xsl:value-of select="$topicref"/></xsl:message>
	<xsl:choose>
		<xsl:when test="$map/map/topicref/topicref/topicref[@href=$topicref]">
			<xsl:message>Got 3rd level topic ref</xsl:message>
			<xsl:variable name="grandparentref"
				select="$map/map/topicref[topicref/topicref[@href=$topicref]]/@href"/>
			<xsl:variable name="parentref" select="$map/map/topicref/topicref[topicref[@href=$topicref]]/@href"/>
			<a href="{$grandparentref}.html#{$topicref}">
				<xsl:apply-templates/>
			</a>
		</xsl:when>
		<xsl:when test="$map/map/topicref/topicref[@href=$topicref]">
			<xsl:message>Got 2nd level topic ref</xsl:message>
			<xsl:variable name="parentref"
				select="$map/map/topicref[topicref[@href=$topicref]]/@href"/>
			<a href="{$parentref}.html#{$topicref}">
				<xsl:apply-templates/>
			</a>
		</xsl:when>
		<xsl:otherwise>
			<xsl:message>Got top level topic ref</xsl:message>
			<a href="{@href}.html">
				<xsl:apply-templates/>
			</a>
		</xsl:otherwise>
	</xsl:choose>
  </xsl:template>

  <!-- If an image has a caption or title, display as a figure block -->
  <xsl:template match="image[caption|title]">
    <figure>
      <img src="images/{@src}">
        <xsl:if test="@id">
          <xsl:attribute name="id">
            <xsl:value-of select="@id"/>
          </xsl:attribute>
        </xsl:if>
        <xsl:attribute name="alt">
          <xsl:value-of select="title"/>
        </xsl:attribute>
      </img>
      <xsl:choose>
        <xsl:when test="caption">
          <figcaption>
            <xsl:value-of select="caption"/>
          </figcaption>
        </xsl:when>
        <xsl:when test="title">
          <figcaption>
            <xsl:value-of select="title"/>
          </figcaption>
        </xsl:when>
      </xsl:choose>
    </figure>
  </xsl:template>

  <!-- An image without a caption or title is displayed inline (e.g. for icons) -->
  <xsl:template match="image">
	<img src="images/{@src}">
        <xsl:if test="@id">
          <xsl:attribute name="id">
            <xsl:value-of select="@id"/>
          </xsl:attribute>
        </xsl:if>
        <xsl:attribute name="alt">
          <xsl:value-of select="title"/>
        </xsl:attribute>
      </img>
  </xsl:template>
  
  <xsl:template match="text[@style]">
    <span style="{@style}">
      <xsl:apply-templates/>
    </span>
  </xsl:template>

  <xsl:template match="@* | node()">
    <xsl:apply-templates select="@* | node()"/>
  </xsl:template>

  <xsl:template match="text()">
    <xsl:copy/>
  </xsl:template>
</xsl:stylesheet>
