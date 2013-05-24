<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
    <xsl:output method="text" indent="yes"/>
    <xsl:param name="ProjectName">BrightstarDB</xsl:param>
  
  <xsl:template match="map">
    <xsl:text>=============================================================&#xA;</xsl:text>
<xsl:value-of select="$ProjectName"/> Documentation<xsl:value-of select="'&#xA;'"/>
<xsl:text>=============================================================</xsl:text>

.. toctree::
   :maxdepth: 3
   
<xsl:apply-templates select="topicref"/>
  
================================
Indices and Tables
================================

* :ref:`genindex`
* :ref:`modindex`
* :ref:`search`
  </xsl:template>

<xsl:template match="topicref"><xsl:value-of select="concat('    ', caption, ' &lt;', @href, '&gt;&#xA;')"/></xsl:template>
</xsl:stylesheet>
