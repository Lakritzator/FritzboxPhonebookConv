<?xml version="1.0" encoding="UTF-8"?>
<!--
  Sample XSLT: Convert a Fritz.Box phonebook XML into a simplified contacts XML.

  Fritz.Box phonebook source format (abbreviated):
  ================================================
  <phonebooks>
    <phonebook name="Telefonbuch">
      <contact>
        <person>
          <realName>Max Mustermann</realName>
        </person>
        <telephony nid="1">
          <number type="home" prio="1" id="0">030 12345678</number>
          <number type="mobile" prio="0" id="1">0171 987654</number>
        </telephony>
      </contact>
      ...
    </phonebook>
  </phonebooks>

  Output format produced by this stylesheet:
  ==========================================
  <contacts source="Telefonbuch">
    <contact>
      <name>Max Mustermann</name>
      <phone type="home">030 12345678</phone>
      <phone type="mobile">0171 987654</phone>
    </contact>
    ...
  </contacts>
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
  <xsl:strip-space elements="*"/>

  <!-- Root template -->
  <xsl:template match="/">
    <contacts source="{//phonebook/@name}">
      <xsl:apply-templates select="//contact"/>
    </contacts>
  </xsl:template>

  <!-- One <contact> element per Fritz.Box contact -->
  <xsl:template match="contact">
    <contact>
      <name>
        <xsl:value-of select="person/realName"/>
      </name>
      <xsl:apply-templates select="telephony/number"/>
    </contact>
  </xsl:template>

  <!-- One <phone> per telephone number -->
  <xsl:template match="telephony/number">
    <phone>
      <xsl:attribute name="type">
        <xsl:value-of select="@type"/>
      </xsl:attribute>
      <xsl:value-of select="."/>
    </phone>
  </xsl:template>

</xsl:stylesheet>
