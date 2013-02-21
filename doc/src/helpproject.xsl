<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
			      xmlns:xi="http://www.w3.org/2001/XInclude"
                              xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"  
>

<xsl:param name="projectfile" select="document('BrightstarDB.hmxp')" />

<xsl:variable name="projectpath"> 
  <xsl:for-each select="topic">
    <xsl:value-of select="substring-before(@xsi:noNamespaceSchemaLocation, 'helpproject.xsd')" />
  </xsl:for-each>
</xsl:variable>

<xsl:variable name="searchpath">
  <xsl:value-of select="translate($projectfile/helpproject/config/config-group[@name='project']/config-value[@name='searchpath'],'\','/')"/>
</xsl:variable>

<xsl:variable name="imagepath">
.<xsl:value-of select="substring-before($searchpath,';')"/>
</xsl:variable>

<xsl:template match="include">
<!-- <xsl:apply-templates select="document(@href)"/> -->
</xsl:template>

<xsl:template name="textstyleclass">
  <xsl:variable name="thisstyle">
    <xsl:value-of select="@styleclass"/>
  </xsl:variable>
  <xsl:attribute name="style">
    <xsl:value-of select="$projectfile/helpproject/config/styleclasses/styleclass[@name=$thisstyle]/style-set[@type='text']"/>;<xsl:value-of select="@style"/>
  </xsl:attribute>
</xsl:template>

<xsl:template name="parastyleclass">
  <xsl:variable name="thisstyle">
    <xsl:value-of select="@styleclass"/>
  </xsl:variable>
  <xsl:attribute name="style">
    <xsl:value-of select="$projectfile/helpproject/config/styleclasses/tablestyleclass[@name=$thisstyle]/style-set[@type='para']"/>;<xsl:value-of select="@style"/>
  </xsl:attribute>
</xsl:template>

<xsl:template name="tablestyleclass">
  <xsl:variable name="thisstyle">
    <xsl:value-of select="@styleclass"/>
  </xsl:variable>
  <xsl:attribute name="style">
    <xsl:value-of select="$projectfile/helpproject/config/styleclasses/styleclass[@name=$thisstyle]/style-set[@type='para']"/>;<xsl:value-of select="@style"/>
  </xsl:attribute>
</xsl:template>

<xsl:template match="/">
  <html>
  <xsl:value-of disable-output-escaping="yes" select="'&lt;!-- saved from url=(0029)http://www.helpandmanual.com/ --&gt;'"/>
  <head>
    <title><xsl:value-of select="@title" /></title>
    <style type="text/css">
      body, p, table, div
           {font-size: 9pt;
 	    font-family: 'Arial';
	    font-style: normal;
            font-weight: normal;
            color: #000000;
            text-decoration: none;
	    text-align: left;
	    text-indent: 0px;
	    padding: 0px 0px 0px 0px;
	    margin: 4px;
	    line-height: normal;
           }
      h1 {font-size: 14pt; font-weight: bold;}
    </style>

    <script language="javascript">
    var s1 = '<xsl:value-of select="$projectpath" />';
    var s2 = '<xsl:value-of select="$searchpath" />' + ';./Baggage/';
    var s3 = s2.split(';');

    function imageError(theImage) {
      var p = 0;
      if (theImage.getAttribute("pathno") == null) { 
        p = 1; 
      } 
      else  { 
        p = parseInt(theImage.getAttribute("pathno"))+1; 
      }
      theImage.setAttribute("pathno", p);
      if (p &lt; s3.length) {
        filename = theImage.src.substring(theImage.src.lastIndexOf('/')+1); 
	theImage.src = s1.substring(0, s1.length-1) + s3[p].substring(1, s3[p].length) + filename;  
      }
    }

    function tblload() {
      var tables = document.getElementsByTagName("table");
      for (var i = 0; i &lt; tables.length; i++) {
        if (tables[i].style["backgroundImage"] != "") { 
          filename = tables[i].style["backgroundImage"].substr(4, tables[i].style["backgroundImage"].length-5);
          var myimg = new Image();
          myimg.onerror = function() { imageError(myimg) };
          myimg.src = s1.substring(0, s1.length-1) + s3[0].substring(1, s3[0].length) + filename; 
          tables[i].style["backgroundImage"] = "url('" + myimg.src + "')"; 
        }
      }
    }

    </script>

    </head>
    <body onload="tblload()">
      <xsl:apply-templates />
    </body>
  </html>
</xsl:template>


<xsl:template match="helpproject">
<!-- do not display config section in preview -->
</xsl:template>

<xsl:template match="topic">
 <table border="0" cellpadding="0" style="margin-left:10px;margin-bottom:40px;">
   <tr><td>
   <table border="0" cellpadding="0" cellspacing="0" style="vertical-align:top;margin-bottom:10px">
     <xsl:if test="./title">
     <tr>
       <td style="width:120px">Topic Title:</td><td style="font-weight:bold"><xsl:value-of select="./title"/></td>
     </tr>
     </xsl:if>
     <tr>
       <td style="width:120px">Modified by:</td><td style="font-weight:bold"><xsl:value-of select="@lasteditedby"/></td>
     </tr>
     <tr>
       <td style="width:120px">Template:</td><td style="font-weight:bold"><xsl:value-of select="@template"/></td>
     </tr>
     <xsl:if test="./keywords">
       <tr style="vertical-align:top">
         <td style="width:120px">Keywords:</td><td>
           <xsl:for-each select="./keywords/keyword">
             <p><xsl:value-of select="text()" /></p>
             <xsl:for-each select="./keyword">
               <p style="margin-left:30px"><xsl:value-of select="text()" /></p>
             </xsl:for-each> 
           </xsl:for-each>
          </td>
       </tr>
     </xsl:if>
     <xsl:if test="./a-keywords">
       <tr style="vertical-align:top">
         <td style="width:120px">A-Keywords:</td><td>
           <xsl:for-each select="./a-keywords/a-keyword">
             <p><xsl:value-of select="." /></p>
           </xsl:for-each>
          </td>
       </tr>
     </xsl:if>
   </table>
   </td></tr>
   <tr style="vertical-align:top">
     <td><xsl:apply-templates/></td>
   </tr> 
 </table> 
</xsl:template>


<!-- Ignore, already processed above -->
<xsl:template match="topic/title">
</xsl:template>
<xsl:template match="topic/keywords">
</xsl:template>
<xsl:template match="topic/a-keywords">
</xsl:template>

<xsl:template match="topic/body">
 
 <table cellpadding="0" cellspacing="0" border="0">
   <tr style="vertical-align:top">
     <td style="padding:4px 4px 4px 4px;margin:0px;border:1px solid #000000;">
       <xsl:apply-templates/>
     </td>
   </tr>
  </table> 
</xsl:template>

<xsl:template match="body/header">
 <div style="background-color:#D3D3D3;padding:4px;margin:-4px -4px 4px -4px">
   <xsl:apply-templates/>
 </div>
</xsl:template>

<xsl:preserve-space elements="text para link var" />

<xsl:template match="para">
 <p style="{@style}"><xsl:call-template name="parastyleclass"/><xsl:apply-templates/><xsl:if test=". = ''">&#160;</xsl:if></p>
</xsl:template>

<xsl:template match="text">
 <span style="{@style};white-space:pre"><xsl:call-template name="textstyleclass"/><xsl:apply-templates/></span>
</xsl:template>

<xsl:template match="list">
 <xsl:choose> 
   <xsl:when test="@type = 'ol'"><ol style="list-style-type:{@listtype}"><xsl:apply-templates/></ol></xsl:when>
   <xsl:otherwise><ul><xsl:apply-templates/></ul></xsl:otherwise>
 </xsl:choose> 
</xsl:template>

<xsl:template match="li">
 <li style="{@style}"><xsl:call-template name="parastyleclass"/><xsl:apply-templates/></li>
</xsl:template>

<xsl:template match="image">
  <xsl:variable name="caption">
    <xsl:copy-of select="./caption"/>
  </xsl:variable>
  <xsl:choose>
    <xsl:when test="./caption or @align != ''">
      <table align="{@align}" cellpadding="0" cellspacing="0" style="text-align:center"><tr style="valign:top"><td style="{@style}">
        <xsl:call-template name="textstyleclass"/>
        <xsl:choose>
          <xsl:when test="@width">
            <img src="{$imagepath}{@src}" onError="imageError(this)" width="{@width}" height="{@height}" style="{@style}"/><br/><xsl:value-of select="$caption"/>
          </xsl:when>
          <xsl:otherwise>
            <img src="{$imagepath}{@src}" onError="imageError(this)" style="{@style}"/><br/><xsl:value-of select="$caption"/>
          </xsl:otherwise>
        </xsl:choose>
      </td></tr></table>
    </xsl:when>
    <xsl:otherwise>
        <xsl:choose>
          <xsl:when test="@width">
            <img src="{$imagepath}{@src}" onError="imageError(this)" width="{@width}" height="{@height}" style="{@style}"/>
          </xsl:when>
          <xsl:otherwise>
            <img src="{$imagepath}{@src}" onError="imageError(this)" style="{@style}"/>
          </xsl:otherwise>
        </xsl:choose>
    </xsl:otherwise>
  </xsl:choose>
</xsl:template>

<xsl:template match="link">
  <xsl:variable name="onclick">
    <xsl:choose>
      <xsl:when test="@type = 'topiclink'">Topic link: href=<xsl:value-of select="@href"/><xsl:if test="@anchor">&#160;&#160;#<xsl:value-of select="@anchor"/></xsl:if></xsl:when>
      <xsl:when test="@type = 'weblink'">Web link: href=<xsl:value-of select="@href"/><xsl:if test="@target">&#160;&#160;Target=<xsl:value-of select="@target"/></xsl:if></xsl:when>
      <xsl:when test="@type = 'filelink'">File link: href=<xsl:value-of select="@href"/><xsl:if test="@params">&#160;&#160;Params=<xsl:value-of select="@params"/></xsl:if></xsl:when>
      <xsl:otherwise>Javascript or Macro</xsl:otherwise>
    </xsl:choose>
  </xsl:variable>
  <xsl:variable name="title">
    <xsl:value-of select="./title"/>
  </xsl:variable>
  <xsl:choose>
    <xsl:when test="@displaytype = 'button'">
      <xsl:variable name="buttoncaption">
        <xsl:value-of select="."/>
      </xsl:variable>
      <input type="button" style="font-family:'MS Sans Serif';font-size:8pt;color:#000000" value="{$buttoncaption}" alt="{$title}" OnClick="javascript:alert('{$onclick}')"/>
    </xsl:when>
    <xsl:when test="@displaytype = 'image'">
      <a href="javascript:void(0);" OnClick="javascript:alert('{$onclick}')"><img src="{$imagepath}{@src}" onError="imageError(this)" alt="{$title}" /></a>
    </xsl:when>
    <xsl:otherwise>
      <a href="javascript:void(0);" style="{@style}" alt="{$title}" OnClick="javascript:alert('{$onclick}')"><xsl:call-template name="textstyleclass"/><xsl:value-of select="."/></a>
    </xsl:otherwise>
  </xsl:choose>
</xsl:template>

<xsl:template match="html-code">
<div style="font-family:'Courier New';font-size:7pt;color:#000000;margin:4px;padding:4px;background-color:#B3D9FF;border:1px dashed #000000">
  <xsl:value-of select="."/>
</div>
</xsl:template>

<xsl:template match="anchor">
<a name="{@id}" style="border:1px dashed #A0A0A0;padding:1px;background-color:#0000FF;color:#FFFFFF;font-weight:bold">Anchor: <xsl:value-of select="@id"/></a>
</xsl:template>

<xsl:template match="line">
<hr style="{@style}"/>
</xsl:template>

<xsl:template match="var">
 <span style="{@style}"><xsl:call-template name="textstyleclass"/><span style="white-space:pre;background-color:#00FF00;"><xsl:value-of select="."/></span></span>
</xsl:template>

<xsl:template match="conditional-text">
<span style="font-weight:bold;color:#FFFFFF;background-color:#FF6868">
  <xsl:choose>
    <xsl:when test="@type = 'IF'">IF&#160;<xsl:value-of select="@value"/>&gt;</xsl:when>
    <xsl:when test="@type = 'IFNOT'">IFNOT&#160;<xsl:value-of select="@value"/>&gt;</xsl:when>
    <xsl:when test="@type = 'ELSE'">&lt;ELSE&gt;</xsl:when>
    <xsl:when test="@type = 'END'">&lt;END</xsl:when>
    <!-- if somebody modified the type attribute to lowercase, it still works -->
    <xsl:otherwise><xsl:value-of select="@type"/><xsl:value-of select="@value"/></xsl:otherwise>
  </xsl:choose>
</span>
</xsl:template>

<xsl:template match="draft-comment">
<div style="white-space:pre;margin:4px;padding:4px;background-color:#FCFCAC;border:1px dashed #000000">
  <b>Draft Comment, <xsl:value-of select="@modified"/>:</b><br/>
  <span style="{@style}"><xsl:call-template name="textstyleclass"/>
    <xsl:value-of select="."/>
  </span>
</div>
</xsl:template>

<xsl:template match="snippet">
<div style="width:100%;text-align:center;padding:4px;background-color:#E2E2E2;border:1px dashed #000000">
Snippet: <b><xsl:value-of select="@src"/></b>
</div>
</xsl:template>

<xsl:template match="tab">
&#160;&#160;&#160;&#160;&#160;
</xsl:template>

<xsl:template match="embedded-image">
<span style="border:2px dashed #000000;padding:2px;font-weight:bold;color:#FFFFFF;background-color:#FF0000">Embedded image</span>
</xsl:template>

<xsl:template match="embedded-olecontrol">
<span style="border:2px dashed #000000;padding:2px;font-weight:bold;color:#FFFFFF;background-color:#FF0000">Embedded OLE control</span>
</xsl:template>

<xsl:template match="toggle">
  <xsl:variable name="caption">
    <xsl:copy-of select="./caption"/>
  </xsl:variable>
  <a href="javascript:alert('Toggle')">
  <xsl:choose>
    <xsl:when test="@type = 'picture'">
      <xsl:choose>
        <xsl:when test="./caption or @align != ''">
          <table align="{@align}" cellpadding="0" cellspacing="0" style="text-align:center"><tr style="valign:top"><td style="{@style}">
            <xsl:call-template name="textstyleclass"/>
            <xsl:choose>
              <xsl:when test="@width">
                <img src="{$imagepath}{@src-collapsed}" onError="imageError(this)" width="{@width}" height="{@height}" style="{@style}"/><br/><xsl:value-of select="$caption"/>
              </xsl:when>
              <xsl:otherwise>
                <img src="{$imagepath}{@src-collapsed}" onError="imageError(this)" style="{@style}"/><br/><xsl:value-of select="$caption"/>
              </xsl:otherwise>
            </xsl:choose>
          </td></tr></table>
        </xsl:when>
        <xsl:otherwise>
            <xsl:choose>
              <xsl:when test="@width">
                <img src="{$imagepath}{@src-collapsed}" onError="imageError(this)" width="{@width}" height="{@height}" style="{@style}"/>
              </xsl:when>
              <xsl:otherwise>
                <img src="{$imagepath}{@src-collapsed}" onError="imageError(this)" style="{@style}"/>
              </xsl:otherwise>
            </xsl:choose>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:when>
    <xsl:otherwise>
      <span style="{@style};white-space:pre"><xsl:call-template name="textstyleclass"/><xsl:apply-templates/></span>
    </xsl:otherwise>
  </xsl:choose>
  </a>
</xsl:template>

<xsl:template match="video">
  <img src="{$imagepath}{@previewimage}" onError="imageError(this)" width="{@width}" height="{@height}" />
</xsl:template>

<xsl:template match="table">
 <table border="1" style="{@style}"><xsl:call-template name="tablestyleclass"/><xsl:apply-templates /></table>
</xsl:template>
<xsl:template match="tr">
 <tr style="{@style}"><xsl:apply-templates /></tr>
</xsl:template>
<xsl:template match="thead">
 <tr style="{@style}"><xsl:apply-templates /></tr>
</xsl:template>
<xsl:template match="td">
 <td colspan="{@colspan}" rowspan="{@rowspan}" style="{@style}"><xsl:apply-templates /></td>
</xsl:template>

</xsl:stylesheet>
