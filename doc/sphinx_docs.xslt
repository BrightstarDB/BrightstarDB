<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
  <xsl:param name="docname"/>
  <xsl:output method="text"/>
  <xsl:strip-space elements="*"/>
  <xsl:variable name="topicpath">src/Topics</xsl:variable>
  
  <xsl:template match="map">
    <xsl:variable name="self" select="current()"/>
    <xsl:variable name="docpath"><xsl:value-of select="$topicpath"/>/<xsl:value-of select="$docname"/>.xml</xsl:variable>
    <xsl:variable name="topic" select="document($topicpath)"/>
    <xsl:value-of select="concat('.. _', $docname, ':&#xA;&#xA;')"/>
    <xsl:call-template name="title">
      <xsl:with-param name="title" select="topicref[@href=$docname]/caption"/>
      <xsl:with-param name="doc.level" select="1"/>
    </xsl:call-template>
    <xsl:call-template name="render-topic">
      <xsl:with-param name="docname" select="$docname"/>
      <xsl:with-param name="doc.level" select="1"/>
    </xsl:call-template>
    <xsl:apply-templates select="topicref[@href=$docname]/topicref">
      <xsl:with-param name="doc.level" select="2"/>
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="topicref">
    <xsl:param name="doc.level"/>
    <xsl:call-template name="start-section">
      <xsl:with-param name="sectionref" select="@href"/>
      <xsl:with-param name="title" select="caption"/>
      <xsl:with-param name="doc.level" select="$doc.level"/>
    </xsl:call-template>
    <xsl:call-template name="render-topic">
      <xsl:with-param name="doc.level" select="$doc.level"/>
      <xsl:with-param name="docname" select="@href"/>
    </xsl:call-template>
    <xsl:apply-templates select="topicref">
      <xsl:with-param name="doc.level" select="$doc.level + 1"/>
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template name="render-topic">
    <xsl:param name="docname" select="''"/>
    <xsl:param name="doc.level" select="1"/>

    <xsl:variable name="docpath"><xsl:value-of select="$topicpath"/>/<xsl:value-of select="$docname"/>.xml</xsl:variable>
    <xsl:variable name="topic" select="document($docpath)"/>

    <xsl:if test="$topic//link[@type='weblink']">
      <xsl:value-of select="'&#xA;&#xA;'"/>
      <xsl:apply-templates select="$topic//link[@type='weblink']" mode="label"/>
    </xsl:if>
    
    <xsl:apply-templates select="$topic/topic/body">
      <xsl:with-param name="doc.level" select="$doc.level"/>
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="header"><!--suppress--></xsl:template>

  <!--
  <xsl:template match="para[not(text)]" priority="1"/>
  
  <xsl:template match="para[@styleclass='Normal']">
    <xsl:if test="text">
      <xsl:value-of select="'&#xA;'"/>
      <xsl:apply-templates/>
      <xsl:if test="following-sibling::para[text][1]/@styleclass='Code Example'">
        <xsl:value-of select="'::&#xA;'"/>
      </xsl:if>
      <xsl:value-of select="'&#xA;'"/>
    </xsl:if>
  </xsl:template>

  <xsl:template match="para[@styleclass='Code Example']">
    <xsl:value-of select="'    '"/><xsl:apply-templates/><xsl:value-of select="'&#xA;'"/>
  </xsl:template>
  -->

  <xsl:template match="para">
    <xsl:choose>
      <xsl:when test="text">
        <xsl:choose>
          <xsl:when test="@styleclass='Code Example' and not (preceding-sibling::para[1]/@styleclass='Code Example')">
            <xsl:value-of select="'::&#xA;&#xA;'"/>
          </xsl:when>
          <xsl:when test="@styleclass='Code Example' and (preceding-sibling::para[1]/@styleclass='Code Example')">
            <xsl:value-of select="'&#xA;'"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="'&#xA;&#xA;'"/>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:if test="@styleclass='Code Example'">
          <xsl:value-of select="'  '"/>
        </xsl:if>
        <xsl:apply-templates/>
      </xsl:when>
      <xsl:when test="table">
        <xsl:value-of select="'&#xA;&#xA;'"/>
        <xsl:apply-templates select="table"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="'&#xA;&#xA;'"/>
        <xsl:apply-templates/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="para[@styleclass='Notes' and text]">
    <xsl:value-of select="'&#xA;&#xA;'"/>
    <xsl:text>.. note::</xsl:text>
    <xsl:value-of select="'&#xA;&#xA;  '"/>
    <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template match="para[@styleclass='Heading1' and text]">
    <xsl:param name="doc.level"/>
    <xsl:call-template name="start-section">
      <xsl:with-param name="title" select="text"/>
      <xsl:with-param name="doc.level" select="$doc.level + 1"/>
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="table">
    <!-- Generating tables that will need manual cleanup-->
    <xsl:apply-templates/>
    <xsl:apply-templates select="thead/td" mode ="line"/>
  </xsl:template>
  
  <xsl:template match="thead">
    <xsl:apply-templates select="td" mode="line"/>
    <xsl:value-of select="'&#xA;'"/>
    <xsl:apply-templates select="td"/>
    <xsl:value-of select="'&#xA;'"/>
    <xsl:apply-templates select ="td" mode="line"/>  
  </xsl:template>

  <xsl:template match ="tr">
    <xsl:value-of select="'&#xA;'"/>
    <xsl:apply-templates select="td"/>  
  </xsl:template>
  
  <xsl:template match="table//para">
     <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template match="td">
    <xsl:apply-templates/><xsl:value-of select="'  '"/>
  </xsl:template>
  
    <xsl:template match="td" mode="line">
    <xsl:call-template name="repeat-string">
      <xsl:with-param name="string" select="'='"/>
      <xsl:with-param name="times" select="string-length(.)"/>
    </xsl:call-template>
    <xsl:value-of select="'  '"/>
  </xsl:template>
  
  <xsl:template match="text[@style='font-weight:bold;' or @style='text-decoration:underline;']">
    <xsl:text>**</xsl:text>
    <xsl:apply-templates/>
    <xsl:text>**</xsl:text>
  </xsl:template>

  <xsl:template match="text[@styleclass='Code Example' and not(../@styleclass='Code Example')]">
    <xsl:text>``</xsl:text>
    <xsl:apply-templates/>
    <xsl:text>``</xsl:text>
  </xsl:template>
  <xsl:template match="link[@type='weblink']" mode="label">
    <xsl:value-of select ="concat('.. _', text(), ': ', @href, '&#xA;')"/>
  </xsl:template>
  
  <xsl:template match="link[@type='weblink']">
    <xsl:value-of select="concat('`', text(), '`_')"/>
  </xsl:template>

  <xsl:template match="link[@type='topiclink']">
    <xsl:choose>
      <xsl:when test="text()">
        <xsl:value-of select="concat(':ref:`', text(), ' &lt;', @href, '&gt;`')"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="concat(':ref:`', @href, '`')"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="image">
    <xsl:value-of select="concat('.. image:: ../src/images/', @src)"/>
  </xsl:template>
  
  <xsl:template match="list">
    <xsl:param name="list.level" select="1"/>
    <xsl:apply-templates>
      <xsl:with-param name="list.level" select="$list.level"/>
    </xsl:apply-templates>
  </xsl:template>
  
  <xsl:template match="li">
    <xsl:param name="list.level"/>
    <xsl:value-of select="'&#xA;&#xA;'"/>
    <xsl:call-template name="repeat-string">
      <xsl:with-param name="string" select="' '"/>
      <xsl:with-param name="times" select="2 * $list.level"/>
    </xsl:call-template>
    <xsl:choose>
      <xsl:when test ="../@type='ol'">
        <xsl:if test="position() = 1">
          <xsl:choose>
            <xsl:when test="../@listtype='decimal'">
              <xsl:value-of select="'1. '"/>
            </xsl:when>
          </xsl:choose>
        </xsl:if>
        <xsl:if test="position() &gt; 1">
          <xsl:value-of select="'#. '"/>
        </xsl:if>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="'- '"/>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template name="start-section">
    <xsl:param name="sectionref"/>
    <xsl:param name="title"/>
    <xsl:param name="doc.level"/>
    <xsl:value-of select="'&#xA;&#xA;&#xA;'"/>
    <xsl:if test="$sectionref">
      <xsl:value-of select="concat('.. _', $sectionref, ':&#xA;&#xA;')"/>
    </xsl:if>
    <xsl:call-template name="title">
      <xsl:with-param name="title" select="$title"/>
      <xsl:with-param name="doc.level" select="$doc.level"/>
    </xsl:call-template>
    <xsl:value-of select="'&#xA;'"/>
  </xsl:template>

  <xsl:template name="title">
    <xsl:param name="title"/>
    <xsl:param name="doc.level"/>
    <xsl:variable name="underline.char">
      <xsl:choose>
        <xsl:when test="$doc.level=1">#</xsl:when>
        <xsl:when test="$doc.level=2">*</xsl:when>
        <xsl:when test="$doc.level=3">=</xsl:when>
        <xsl:when test="$doc.level=4">-</xsl:when>
        <xsl:when test="$doc.level=5">^</xsl:when>
        <xsl:when test="$doc.level=6">"</xsl:when>
      </xsl:choose>
    </xsl:variable>
    <xsl:variable name="underline.length">
      <xsl:choose>
        <xsl:when test="$doc.level = 1 or $doc.level=2">
          <xsl:value-of select="string-length($title) + 1"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="string-length($title)"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:variable name="title.prefix">
      <xsl:choose>
        <xsl:when test="$doc.level=1 or $doc.level=2" xml:space="preserve"> </xsl:when>
        <xsl:otherwise></xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:if test="$doc.level=1 or $doc.level=2">
      <xsl:call-template name="repeat-string">
        <xsl:with-param name="string" select="$underline.char"/>
        <xsl:with-param name="times" select="$underline.length"/>
      </xsl:call-template>
    </xsl:if>
    <xsl:choose>
      <xsl:when test="$doc.level=1 or $doc.level=2">
        <xsl:value-of select="concat('&#xA; ', $title, '&#xA;')"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="concat('&#xA;', $title, '&#xA;')"/>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:call-template name="repeat-string">
      <xsl:with-param name="string" select="$underline.char"/>
      <xsl:with-param name="times" select="$underline.length"/>
    </xsl:call-template>
    <xsl:text>&#xA;</xsl:text>
  </xsl:template>

  <xsl:template name="repeat-string">
    <xsl:param name="string" select="''"/>
    <xsl:param name="times" select="1"/>
    <xsl:if test="number($times) &gt; 0">
      <xsl:value-of select="$string"/>
      <xsl:call-template name="repeat-string">
        <xsl:with-param name="string" select="$string"/>
        <xsl:with-param name="times" select="$times - 1"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>
  
  <xsl:template match="@* | node()">
    <xsl:param name="doc.level"/>
        <xsl:copy>
          <xsl:apply-templates select="@* | node()">
            <xsl:with-param name="doc.level" select="$doc.level"/>
          </xsl:apply-templates>
        </xsl:copy>
    </xsl:template>

</xsl:stylesheet>
