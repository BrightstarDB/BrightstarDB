<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
                xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
>
    <xsl:output method="xml" version="1.0" encoding="utf-8" indent="yes" />
  <xsl:variable name="rootDir" select="wix:Wix/wix:Fragment/wix:DirectoryRef[1]/@Id"/>
  <xsl:template match="wix:Wix">
    <xsl:copy>
      <xsl:apply-templates/>
      <wix:Fragment>
        <wix:Component Id="RemoveDirectories" Directory="{$rootDir}">
          <xsl:apply-templates select="//wix:DirectoryRef/wix:Directory" mode="RemoveDir"/>
          <wix:RegistryKey Root="HKCU" Key="Software\BrightstarDB\Uninstall\{$rootDir}">
            <wix:RegistryValue Value="0" Type="integer" KeyPath="yes"/>
          </wix:RegistryKey>
        </wix:Component>
      </wix:Fragment>
    </xsl:copy>
    
  </xsl:template>

  <xsl:template match="wix:Directory" mode="RemoveDir">
    <xsl:variable name="folderId" select="@Id"/>
    <wix:RemoveFolder Id="removeFolder{@Id}" Directory="{@Id}" On="uninstall"/>
  </xsl:template>
  
    <xsl:template match="@* | node()">
        <xsl:copy>
            <xsl:apply-templates select="@* | node()"/>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>
