<?php
    $normalOrder = false;
    ob_start();
?>
<style type="text/css" media="print">
@page {
    size: auto;   /* auto is the initial value */
    margin: 30px;  /* this affects the margin in the printer settings */
}
</style>
<style>
    .a5 {
        border-spacing: 0px;        
    }
    .a5 td {
        border: 0px solid #CCCCCC;  
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
</style>
<!-- <div style="box-shadow: inset 0 0 0 1000px red; height: 10px;" /> -->
<table class="a5" width="1050" align="center" style="font-family: Segoe UI; font-size: 14px;">
<?php
    $content = str_replace("\r", "", str_replace("<br />", "", file_get_contents("README.md")));
    $pos1 = strpos($content, "#");
    $pos2 = strpos($content, "\n",  $pos1);
    $header = substr($content, $pos1 + 2, $pos2 - $pos1 - 2);
    $content = "<span style=\"font-size: 18px; font-weight: bold\">$header</span>".substr($content, $pos2);
    $pos = strpos($content, "---");
    $content = substr($content, 0, $pos);

    preg_match_all("|<!-- page \d+ -->|", $content, $matches);
    $startPos = 0;

    $output = "";

    if ($normalOrder) { // 1 2 3 4, for reading in browser
        for($i = 0; $i < count($matches[0]); $i++) {
            $pos = strpos($content, $matches[0][$i]);
    
            if ($i % 2 == 0) {
                $output.= "<tr><td class=\"left\"><div>".nl2br(trim(substr($content, $startPos, $pos - $startPos)))."</div><div class=\"marking1\"></div><div class=\"marking2\"></div><div class=\"marking3\"></div><div class=\"marking4\"></div><div class=\"marking5\"></div><div class=\"marking6\"></div>\n";
            }
            else {
                $output.= "<td class=\"right\"><div>".nl2br(trim(substr($content, $startPos, $pos - $startPos)))."</div></td></tr>";
            }
            $startPos = $pos + strlen($matches[0][$i]);
        }
        if ($i % 2 == 1) {
            $output.= "<td class=\"right\"></td></tr>";
        }
    }
    else { // 4 1 2 3, for printing on A4 paper on both sides
        $pages = array();
        
        for($i = 0; $i < count($matches[0]); $i++) {
            $pos = strpos($content, $matches[0][$i]);
    
            if ($i % 2 == 1) {
                $pages[] = "<tr><td class=\"left\"><div>".nl2br(trim(substr($content, $startPos, $pos - $startPos)))."</div><div class=\"marking1\"></div><div class=\"marking2\"></div><div class=\"marking3\"></div><div class=\"marking4\"></div><div class=\"marking5\"></div><div class=\"marking6\"></div></td>\n";
            }
            else {
                $pages[] = "<td class=\"right\"><div>".nl2br(trim(substr($content, $startPos, $pos - $startPos)))."</div></td></tr>";
            }
            $startPos = $pos + strlen($matches[0][$i]);
        }
        if ($i % 4 == 1) {
            $pages[] = "";
            $pages[] = ""; 
            $pages[] = "<tr><td class=\"left\"></td>";
        }
        else if ($i % 4 == 2) {
            $pages[] = "<td class=\"right\"></td></tr>";
            $pages[] = "<tr><td class=\"left\"></td>";
        }
        else if ($i % 4 == 3) {
            $pages[] = "<tr><td class=\"left\"></td>";
        }

        $pageNum = count($pages) / 4;

        for ($i = 1; $i <= $pageNum; $i++) {
            $output.= $pages[$i*4 - 1].$pages[$i*4-4].$pages[$i*4-3].$pages[$i*4-2];
        }
    }
    
    print $output;
?> 
</table>
<?php
$htmlStr = ob_get_contents();
file_put_contents("A5print.html", $htmlStr);
?>
