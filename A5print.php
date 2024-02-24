<?php
    if (isset($_GET["order"])) {
        if ($_GET["order"] == 0) {
            $normalOrder = true;        
        }
        else {
            $normalOrder = false;
        }
    }
    else {
        $normalOrder = true;    
    }
    ob_start();
?>
<style type="text/css" media="print">
@page {
    size: auto;   /* auto is the initial value */
    margin: 30px;  /* this affects the margin in the printer settings */
}
</style>
<style>
    .a5, .a5_1 {
        border-spacing: 0px;  
        margin-top: -8px;
    }
    .a5 td {
        border: 0px solid #CCCCCC;  
        width: 525px;    
        height: 725px;  
        vertical-align: top;
        position: relative;
    }
    .a5_1 td {
        border: 0px solid #808080;  
        width: 525px;    
        height: 725px;  
        vertical-align: top;
        position: relative;
    }
    .left {
        padding-left: 0px;
        padding-right: 42px;
    }
    .right {
        padding-left: 42px;
        padding-right: 2px;
    }
    div {
        width: 100%;
        height: 720px;
        overflow: hidden;
        box-shadow: inset 0 0 0 1000px white;      
    }

    div.marking1 {
        position: absolute;
        left: 523px;
        top: 8px;
        width: 2px;
        height: 2px;
        box-shadow: inset 0 0 0 30px gray;        
    }
    div.marking2 {
        position: absolute;
        left: 523px;
        top: 724px;
        width: 2px;
        height: 2px;
        box-shadow: inset 0 0 0 30px gray;        
    }
    div.marking3 {
        position: absolute;
        left: 523px;
        top: 58px;
        width: 2px;
        height: 2px;
        box-shadow: inset 0 0 0 30px gray;        
    }
    div.marking4 {
        position: absolute;
        left: 523px;
        top: 674px;
        width: 2px;
        height: 2px;
        box-shadow: inset 0 0 0 30px gray;        
    }
    div.marking5 {
        position: absolute;
        left: 523px;
        top: 341px;
        width: 2px;
        height: 2px;
        box-shadow: inset 0 0 0 30px gray;        
    }
    div.marking6 {
        position: absolute;
        left: 523px;
        top: 391px;
        width: 2px;
        height: 2px;
        box-shadow: inset 0 0 0 30px gray;        
    }

    div.leftNum {
        position: absolute;
        right: 0px;
        bottom: 0px;
        width: 27px;
        height: 16px;
    }
    div.rightNum {
        position: absolute;
        left: 0px;
        bottom: 0px;
        width: 27px;
        height: 16px;
        text-align: right;
        box-shadow: inset 0 0 0 30px transparent;
    }
</style>
<table class="<?php $normalOrder == true ? print "a5_1" : print "a5" ?>" width="1050" align="center" style="font-family: Segoe UI; font-size: 14px;">
<?php
    $content = str_replace("\r", "", str_replace("<br />", "", file_get_contents("README.md")));
    
    $pos1 = strpos($content, "#");
    $pos2 = strpos($content, "\n",  $pos1);
    $header = substr($content, $pos1 + 2, $pos2 - $pos1 - 2);
    $content = "<span style=\"font-size: 18px; font-weight: bold\">$header</span>".substr($content, $pos2);

    $pos1 = strpos($content, "(");
    $pos2 = strpos($content, ")",  $pos1);
    $content = substr($content, 0, $pos1).substr($content, $pos2 + 3);

    $pos = strpos($content, "---\n");
    $content = substr($content, 0, $pos);

    $pos = 0;
    $endPos = strpos($content, "<!---->", $pos);
    $pageTexts = array();

    while ($endPos != false) {
        $pageTexts[] = nl2br(trim(substr($content, $pos, $endPos - $pos)));
        $pos = $endPos + 8;
        $endPos = strpos($content, "<!---->", $pos);
    }    
    $pageTexts[] = nl2br(trim(substr($content, $pos)));
    
    $output = "";

    if ($normalOrder) { // 1 2 3 4, for reading in browser
        for($i = 0; $i < count($pageTexts); $i++) {
            if ($i % 2 == 0) {                
                $output.= "<tr><td class=\"left\"><div>".$pageTexts[$i]."</div><div class=\"leftNum\">".($i+1)."</div></td>\n";
            }            
            else {
                $output.= "<td class=\"right\"><div>".$pageTexts[$i]."</div><div class=\"rightNum\">".($i+1)."</div></td></tr>\n";
            }
        }
        if ($i % 2 == 1) {
            $output.= "<td class=\"right\"></td></tr>\n";
        }
    }
    else { // 4 1 2 3, for printing on A4 paper on both sides
        $pages = array();
        
        for($i = 0; $i < count($pageTexts); $i++) {
            if ($i % 2 == 1) {
                if ($i % 4 == 1) {
                    $pages[] = "<tr><td class=\"left\"><div>".$pageTexts[$i]."</div><div class=\"leftNum\">".($i+1)."</div><div class=\"marking1\"></div><div class=\"marking2\"></div><div class=\"marking3\"></div><div class=\"marking4\"></div><div class=\"marking5\"></div><div class=\"marking6\"></div></td>\n";
                }
                else {
                    $pages[] = "<tr><td class=\"left\"><div>".$pageTexts[$i]."</div><div class=\"leftNum\">".($i+1)."</div></td>\n";
                }
            }
            else {
                $pages[] = "<td class=\"right\"><div>".$pageTexts[$i]."</div><div class=\"rightNum\">".($i+1)."</div></td></tr>";
            }
        }
        if ($i % 4 == 1) {
            $pages[] = "<tr><td class=\"left\"><div class=\"marking1\"></div><div class=\"marking2\"></div><div class=\"marking3\"></div><div class=\"marking4\"></div><div class=\"marking5\"></div><div class=\"marking6\"></div></td>\n";
            $pages[] = "<td class=\"right\"></td></tr>\n";
            $pages[] = "<tr><td class=\"left\"></td>\n";
        }
        else if ($i % 4 == 2) {
            $pages[] = "<td class=\"right\"></td></tr>\n";
            $pages[] = "<tr><td class=\"left\"></td>\n";
        }
        else if ($i % 4 == 3) {
            $pages[] = "<tr><td class=\"left\"></td>\n";
        }

        $pageNum = count($pages) / 4;

        for ($i = 1; $i <= $pageNum; $i++) {
            $output.= $pages[$i*4 - 1].$pages[$i*4 - 4].$pages[$i*4 - 3].$pages[$i*4 - 2];
        }
    }    
    print $output;
?> 
</table>
<?php
$htmlStr = ob_get_contents();
file_put_contents("A5print.html", $htmlStr);
?>
