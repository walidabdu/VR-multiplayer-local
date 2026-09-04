$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputDir = Join-Path $repoRoot 'Assets\Slides'
$pptxPath = Join-Path $outputDir 'Virtual-Electrical-Lab-Opening-Slide.pptx'
$templatePath = 'C:\Program Files\Microsoft Office\root\Office16\1033\PREVIEWTEMPLATE.POTX'

if (-not (Test-Path $templatePath)) {
    throw "Template not found: $templatePath"
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
Copy-Item -LiteralPath $templatePath -Destination $pptxPath -Force

function Set-ZipTextEntry {
    param(
        [System.IO.Compression.ZipArchive]$Zip,
        [string]$EntryName,
        [string]$Content
    )

    $existing = $Zip.GetEntry($EntryName)
    if ($existing) {
        $existing.Delete()
    }

    $entry = $Zip.CreateEntry($EntryName, [System.IO.Compression.CompressionLevel]::Optimal)
    $writer = [System.IO.StreamWriter]::new($entry.Open(), [System.Text.UTF8Encoding]::new($false))
    $writer.Write($Content)
    $writer.Dispose()
}

$createdUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

$contentTypes = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="jpeg" ContentType="image/jpeg"/>
  <Default Extension="jpg" ContentType="image/jpeg"/>
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Default Extension="xlsx" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"/>
  <Override PartName="/ppt/presentation.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"/>
  <Override PartName="/ppt/slideMasters/slideMaster1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml"/>
  <Override PartName="/ppt/slides/slide1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slide+xml"/>
  <Override PartName="/ppt/slides/slide2.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slide+xml"/>
  <Override PartName="/ppt/slides/slide3.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slide+xml"/>
  <Override PartName="/ppt/slides/slide4.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slide+xml"/>
  <Override PartName="/ppt/presProps.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.presProps+xml"/>
  <Override PartName="/ppt/viewProps.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.viewProps+xml"/>
  <Override PartName="/ppt/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/>
  <Override PartName="/ppt/tableStyles.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.tableStyles+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout1.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout2.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout3.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout4.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout5.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout6.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout7.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout8.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout9.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout10.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/slideLayouts/slideLayout11.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml"/>
  <Override PartName="/ppt/charts/chart1.xml" ContentType="application/vnd.openxmlformats-officedocument.drawingml.chart+xml"/>
  <Override PartName="/ppt/diagrams/data1.xml" ContentType="application/vnd.openxmlformats-officedocument.drawingml.diagramData+xml"/>
  <Override PartName="/ppt/diagrams/layout1.xml" ContentType="application/vnd.openxmlformats-officedocument.drawingml.diagramLayout+xml"/>
  <Override PartName="/ppt/diagrams/quickStyle1.xml" ContentType="application/vnd.openxmlformats-officedocument.drawingml.diagramStyle+xml"/>
  <Override PartName="/ppt/diagrams/colors1.xml" ContentType="application/vnd.openxmlformats-officedocument.drawingml.diagramColors+xml"/>
  <Override PartName="/ppt/diagrams/drawing1.xml" ContentType="application/vnd.ms-office.drawingml.diagramDrawing+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
  <Override PartName="/ppt/charts/colors1.xml" ContentType="application/vnd.ms-office.chartcolorstyle+xml"/>
  <Override PartName="/ppt/charts/style1.xml" ContentType="application/vnd.ms-office.chartstyle+xml"/>
</Types>
'@

$appXml = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Microsoft Office PowerPoint</Application>
  <PresentationFormat>Widescreen</PresentationFormat>
  <Slides>1</Slides>
  <Notes>0</Notes>
  <HiddenSlides>0</HiddenSlides>
  <MMClips>0</MMClips>
  <ScaleCrop>false</ScaleCrop>
  <HeadingPairs>
    <vt:vector size="2" baseType="variant">
      <vt:variant>
        <vt:lpstr>Theme</vt:lpstr>
      </vt:variant>
      <vt:variant>
        <vt:i4>1</vt:i4>
      </vt:variant>
    </vt:vector>
  </HeadingPairs>
  <TitlesOfParts>
    <vt:vector size="1" baseType="lpstr">
      <vt:lpstr>Virtual Electrical Lab Opening Slide</vt:lpstr>
    </vt:vector>
  </TitlesOfParts>
  <Company>OpenAI Codex</Company>
  <LinksUpToDate>false</LinksUpToDate>
  <SharedDoc>false</SharedDoc>
  <HyperlinksChanged>false</HyperlinksChanged>
  <AppVersion>16.0000</AppVersion>
</Properties>
'@

$coreXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>Virtual Electrical Lab Opening Slide</dc:title>
  <dc:subject>Opening Slide</dc:subject>
  <dc:creator>OpenAI Codex</dc:creator>
  <cp:keywords>virtual electrical lab, powerpoint, opening slide</cp:keywords>
  <dc:description>Single slide PowerPoint opening slide for Virtual Electrical Lab.</dc:description>
  <cp:lastModifiedBy>OpenAI Codex</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">$createdUtc</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">$createdUtc</dcterms:modified>
</cp:coreProperties>
"@

$presentationXml = @'
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<p:presentation xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" removePersonalInfoOnSave="1" saveSubsetFonts="1">
  <p:sldMasterIdLst>
    <p:sldMasterId id="2147483648" r:id="rId1"/>
  </p:sldMasterIdLst>
  <p:sldIdLst>
    <p:sldId id="256" r:id="rId2"/>
  </p:sldIdLst>
  <p:sldSz cx="12192000" cy="6858000"/>
  <p:notesSz cx="6858000" cy="9144000"/>
  <p:defaultTextStyle>
    <a:defPPr>
      <a:defRPr lang="en-US"/>
    </a:defPPr>
    <a:lvl1pPr marL="0" algn="l" defTabSz="914400" rtl="0" eaLnBrk="1" latinLnBrk="0" hangingPunct="1">
      <a:defRPr sz="1800" kern="1200">
        <a:solidFill><a:schemeClr val="tx1"/></a:solidFill>
        <a:latin typeface="+mn-lt"/>
        <a:ea typeface="+mn-ea"/>
        <a:cs typeface="+mn-cs"/>
      </a:defRPr>
    </a:lvl1pPr>
  </p:defaultTextStyle>
</p:presentation>
'@

$presentationRels = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster" Target="slideMasters/slideMaster1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide" Target="slides/slide1.xml"/>
  <Relationship Id="rId6" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/presProps" Target="presProps.xml"/>
  <Relationship Id="rId7" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/viewProps" Target="viewProps.xml"/>
  <Relationship Id="rId8" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme" Target="theme/theme1.xml"/>
  <Relationship Id="rId9" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/tableStyles" Target="tableStyles.xml"/>
</Relationships>
'@

$slideRels = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout" Target="../slideLayouts/slideLayout7.xml"/>
</Relationships>
'@

$slideXml = @'
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
  <p:cSld name="Virtual Electrical Lab Opening Slide">
    <p:spTree>
      <p:nvGrpSpPr>
        <p:cNvPr id="1" name=""/>
        <p:cNvGrpSpPr/>
        <p:nvPr/>
      </p:nvGrpSpPr>
      <p:grpSpPr>
        <a:xfrm>
          <a:off x="0" y="0"/>
          <a:ext cx="0" cy="0"/>
          <a:chOff x="0" y="0"/>
          <a:chExt cx="0" cy="0"/>
        </a:xfrm>
      </p:grpSpPr>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="2" name="Background"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="0" y="0"/><a:ext cx="12192000" cy="6858000"/></a:xfrm>
          <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="07111F"/></a:solidFill>
          <a:ln><a:noFill/></a:ln>
        </p:spPr>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="3" name="Left Glow"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="900000" y="650000"/><a:ext cx="3100000" cy="3100000"/></a:xfrm>
          <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="13D7FF"><a:alpha val="14000"/></a:srgbClr></a:solidFill>
          <a:ln><a:noFill/></a:ln>
        </p:spPr>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="4" name="Right Glow"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="9050000" y="1200000"/><a:ext cx="2450000" cy="2450000"/></a:xfrm>
          <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="FFB341"><a:alpha val="12000"/></a:srgbClr></a:solidFill>
          <a:ln><a:noFill/></a:ln>
        </p:spPr>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="5" name="Header Chip"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="762000" y="762000"/><a:ext cx="2800000" cy="406000"/></a:xfrm>
          <a:prstGeom prst="roundRect"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="10253D"/></a:solidFill>
          <a:ln w="12700">
            <a:solidFill><a:srgbClr val="4EDFFF"><a:alpha val="35000"/></a:srgbClr></a:solidFill>
          </a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr wrap="none" anchor="ctr" lIns="228600" tIns="0" rIns="228600" bIns="0"/>
          <a:lstStyle/>
          <a:p>
            <a:pPr algn="ctr"/>
            <a:r>
              <a:rPr lang="en-US" sz="1700" b="1">
                <a:solidFill><a:srgbClr val="C7F3FF"/></a:solidFill>
                <a:latin typeface="Aptos"/>
              </a:rPr>
              <a:t>IMMERSIVE STEM EXPERIENCE</a:t>
            </a:r>
            <a:endParaRPr lang="en-US" sz="1700"/>
          </a:p>
        </p:txBody>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="6" name="Title Virtual"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="762000" y="1320000"/><a:ext cx="4700000" cy="730000"/></a:xfrm>
          <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
          <a:noFill/>
          <a:ln><a:noFill/></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr wrap="none" anchor="ctr" lIns="0" tIns="0" rIns="0" bIns="0"/>
          <a:lstStyle/>
          <a:p>
            <a:pPr algn="l"/>
            <a:r>
              <a:rPr lang="en-US" sz="7600" b="1" kern="0">
                <a:solidFill><a:srgbClr val="EAF8FF"/></a:solidFill>
                <a:latin typeface="Bahnschrift"/>
              </a:rPr>
              <a:t>VIRTUAL</a:t>
            </a:r>
            <a:endParaRPr lang="en-US" sz="7600"/>
          </a:p>
        </p:txBody>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="7" name="Title Electrical"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="762000" y="2050000"/><a:ext cx="5200000" cy="730000"/></a:xfrm>
          <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
          <a:noFill/>
          <a:ln><a:noFill/></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr wrap="none" anchor="ctr" lIns="0" tIns="0" rIns="0" bIns="0"/>
          <a:lstStyle/>
          <a:p>
            <a:pPr algn="l"/>
            <a:r>
              <a:rPr lang="en-US" sz="7600" b="1" kern="0">
                <a:solidFill><a:srgbClr val="FFB341"/></a:solidFill>
                <a:latin typeface="Bahnschrift"/>
              </a:rPr>
              <a:t>ELECTRICAL</a:t>
            </a:r>
            <a:endParaRPr lang="en-US" sz="7600"/>
          </a:p>
        </p:txBody>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="8" name="Title Lab"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="762000" y="2780000"/><a:ext cx="3600000" cy="730000"/></a:xfrm>
          <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
          <a:noFill/>
          <a:ln><a:noFill/></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr wrap="none" anchor="ctr" lIns="0" tIns="0" rIns="0" bIns="0"/>
          <a:lstStyle/>
          <a:p>
            <a:pPr algn="l"/>
            <a:r>
              <a:rPr lang="en-US" sz="7600" b="1" kern="0">
                <a:solidFill><a:srgbClr val="EAF8FF"/></a:solidFill>
                <a:latin typeface="Bahnschrift"/>
              </a:rPr>
              <a:t>LAB</a:t>
            </a:r>
            <a:endParaRPr lang="en-US" sz="7600"/>
          </a:p>
        </p:txBody>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="9" name="Subtitle"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="800000" y="3750000"/><a:ext cx="5200000" cy="760000"/></a:xfrm>
          <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
          <a:noFill/>
          <a:ln><a:noFill/></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr wrap="square" anchor="t" lIns="0" tIns="0" rIns="0" bIns="0"/>
          <a:lstStyle/>
          <a:p>
            <a:r>
              <a:rPr lang="en-US" sz="2200">
                <a:solidFill><a:srgbClr val="D9EEFF"/></a:solidFill>
                <a:latin typeface="Aptos"/>
              </a:rPr>
              <a:t>A VR-powered environment for building circuits, testing ideas, and learning electrical concepts through safe, interactive experiments.</a:t>
            </a:r>
            <a:endParaRPr lang="en-US" sz="2200"/>
          </a:p>
        </p:txBody>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="10" name="Divider"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="762000" y="4580000"/><a:ext cx="3500000" cy="12700"/></a:xfrm>
          <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="6CDFFF"><a:alpha val="25000"/></a:srgbClr></a:solidFill>
          <a:ln><a:noFill/></a:ln>
        </p:spPr>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="11" name="Tag 1"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="762000" y="4900000"/><a:ext cx="1550000" cy="360000"/></a:xfrm>
          <a:prstGeom prst="roundRect"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="11243C"/></a:solidFill>
          <a:ln w="12700"><a:solidFill><a:srgbClr val="42DBFF"><a:alpha val="35000"/></a:srgbClr></a:solidFill></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr wrap="none" anchor="ctr"/>
          <a:lstStyle/>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="en-US" sz="1800" b="1"><a:solidFill><a:srgbClr val="E7FAFF"/></a:solidFill><a:latin typeface="Aptos"/></a:rPr><a:t>VR-first learning</a:t></a:r><a:endParaRPr lang="en-US" sz="1800"/></a:p>
        </p:txBody>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="12" name="Tag 2"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="2400000" y="4900000"/><a:ext cx="1700000" cy="360000"/></a:xfrm>
          <a:prstGeom prst="roundRect"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="11243C"/></a:solidFill>
          <a:ln w="12700"><a:solidFill><a:srgbClr val="42DBFF"><a:alpha val="35000"/></a:srgbClr></a:solidFill></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr wrap="none" anchor="ctr"/>
          <a:lstStyle/>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="en-US" sz="1800" b="1"><a:solidFill><a:srgbClr val="E7FAFF"/></a:solidFill><a:latin typeface="Aptos"/></a:rPr><a:t>Real-time feedback</a:t></a:r><a:endParaRPr lang="en-US" sz="1800"/></a:p>
        </p:txBody>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="13" name="Tag 3"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="4250000" y="4900000"/><a:ext cx="1850000" cy="360000"/></a:xfrm>
          <a:prstGeom prst="roundRect"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="11243C"/></a:solidFill>
          <a:ln w="12700"><a:solidFill><a:srgbClr val="FFB341"><a:alpha val="40000"/></a:srgbClr></a:solidFill></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr wrap="none" anchor="ctr"/>
          <a:lstStyle/>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="en-US" sz="1800" b="1"><a:solidFill><a:srgbClr val="FFF1D8"/></a:solidFill><a:latin typeface="Aptos"/></a:rPr><a:t>Collaborative practice</a:t></a:r><a:endParaRPr lang="en-US" sz="1800"/></a:p>
        </p:txBody>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="14" name="Right Panel"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="7350000" y="950000"/><a:ext cx="3900000" cy="4400000"/></a:xfrm>
          <a:prstGeom prst="roundRect"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="0B1728"><a:alpha val="78000"/></a:srgbClr></a:solidFill>
          <a:ln w="19050"><a:solidFill><a:srgbClr val="53E1FF"><a:alpha val="50000"/></a:srgbClr></a:solidFill></a:ln>
        </p:spPr>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="15" name="Center Ring Outer"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="8300000" y="1650000"/><a:ext cx="1900000" cy="1900000"/></a:xfrm>
          <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
          <a:noFill/>
          <a:ln w="19050"><a:solidFill><a:srgbClr val="56E1FF"><a:alpha val="26000"/></a:srgbClr></a:solidFill></a:ln>
        </p:spPr>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="16" name="Center Ring Inner"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="8570000" y="1920000"/><a:ext cx="1360000" cy="1360000"/></a:xfrm>
          <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
          <a:noFill/>
          <a:ln w="19050"><a:solidFill><a:srgbClr val="56E1FF"><a:alpha val="18000"/></a:srgbClr></a:solidFill></a:ln>
        </p:spPr>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="17" name="Core Dot"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="9210000" y="2560000"/><a:ext cx="95000" cy="95000"/></a:xfrm>
          <a:prstGeom prst="ellipse"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="FFB341"/></a:solidFill>
          <a:ln><a:noFill/></a:ln>
        </p:spPr>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="18" name="Device Outline"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="8820000" y="2380000"/><a:ext cx="850000" cy="520000"/></a:xfrm>
          <a:prstGeom prst="roundRect"><a:avLst/></a:prstGeom>
          <a:noFill/>
          <a:ln w="38100"><a:solidFill><a:srgbClr val="F3FBFF"/></a:solidFill></a:ln>
        </p:spPr>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="19" name="Voltage Box"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="9850000" y="1380000"/><a:ext cx="1460000" cy="800000"/></a:xfrm>
          <a:prstGeom prst="roundRect"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="0A1525"/></a:solidFill>
          <a:ln w="12700"><a:solidFill><a:srgbClr val="56E1FF"><a:alpha val="28000"/></a:srgbClr></a:solidFill></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr anchor="ctr"/>
          <a:lstStyle/>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="en-US" sz="1400" b="1"><a:solidFill><a:srgbClr val="8FEAFF"/></a:solidFill><a:latin typeface="Aptos"/></a:rPr><a:t>VOLTAGE</a:t></a:r><a:endParaRPr lang="en-US" sz="1400"/></a:p>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="en-US" sz="3200" b="1"><a:solidFill><a:srgbClr val="F2FBFF"/></a:solidFill><a:latin typeface="Bahnschrift"/></a:rPr><a:t>12.0V</a:t></a:r><a:endParaRPr lang="en-US" sz="3200"/></a:p>
        </p:txBody>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="20" name="Current Box"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="7600000" y="3900000"/><a:ext cx="1460000" cy="800000"/></a:xfrm>
          <a:prstGeom prst="roundRect"><a:avLst/></a:prstGeom>
          <a:solidFill><a:srgbClr val="0A1525"/></a:solidFill>
          <a:ln w="12700"><a:solidFill><a:srgbClr val="FFB341"><a:alpha val="32000"/></a:srgbClr></a:solidFill></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr anchor="ctr"/>
          <a:lstStyle/>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="en-US" sz="1400" b="1"><a:solidFill><a:srgbClr val="FFD18A"/></a:solidFill><a:latin typeface="Aptos"/></a:rPr><a:t>CURRENT</a:t></a:r><a:endParaRPr lang="en-US" sz="1400"/></a:p>
          <a:p><a:pPr algn="ctr"/><a:r><a:rPr lang="en-US" sz="3200" b="1"><a:solidFill><a:srgbClr val="FFF7EA"/></a:solidFill><a:latin typeface="Bahnschrift"/></a:rPr><a:t>2.5A</a:t></a:r><a:endParaRPr lang="en-US" sz="3200"/></a:p>
        </p:txBody>
      </p:sp>

      <p:sp>
        <p:nvSpPr><p:cNvPr id="21" name="Footer"/><p:cNvSpPr txBox="1"/><p:nvPr/></p:nvSpPr>
        <p:spPr>
          <a:xfrm><a:off x="8800000" y="6220000"/><a:ext cx="2400000" cy="350000"/></a:xfrm>
          <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
          <a:noFill/>
          <a:ln><a:noFill/></a:ln>
        </p:spPr>
        <p:txBody>
          <a:bodyPr wrap="none" anchor="ctr"/>
          <a:lstStyle/>
          <a:p><a:pPr algn="r"/><a:r><a:rPr lang="en-US" sz="1800"><a:solidFill><a:srgbClr val="DDEFFF"/></a:solidFill><a:latin typeface="Aptos"/></a:rPr><a:t>Virtual Electrical Lab</a:t></a:r><a:endParaRPr lang="en-US" sz="1800"/></a:p>
        </p:txBody>
      </p:sp>
    </p:spTree>
  </p:cSld>
  <p:clrMapOvr>
    <a:masterClrMapping/>
  </p:clrMapOvr>
</p:sld>
'@

$zip = [System.IO.Compression.ZipFile]::Open($pptxPath, [System.IO.Compression.ZipArchiveMode]::Update)
try {
    Set-ZipTextEntry -Zip $zip -EntryName '[Content_Types].xml' -Content $contentTypes
    Set-ZipTextEntry -Zip $zip -EntryName 'docProps/app.xml' -Content $appXml
    Set-ZipTextEntry -Zip $zip -EntryName 'docProps/core.xml' -Content $coreXml
    Set-ZipTextEntry -Zip $zip -EntryName 'ppt/presentation.xml' -Content $presentationXml
    Set-ZipTextEntry -Zip $zip -EntryName 'ppt/_rels/presentation.xml.rels' -Content $presentationRels
    Set-ZipTextEntry -Zip $zip -EntryName 'ppt/slides/slide1.xml' -Content $slideXml
    Set-ZipTextEntry -Zip $zip -EntryName 'ppt/slides/_rels/slide1.xml.rels' -Content $slideRels
}
finally {
    $zip.Dispose()
}

Write-Output $pptxPath
