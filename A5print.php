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
        border: 0px solid gray;  
        width: 525px;    
        height: 725px;  
        vertical-align: top;
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
        /*box-shadow: inset 0 0 0 1000px gold;*/        
    }
</style>
<!-- <div style="box-shadow: inset 0 0 0 1000px red; height: 10px;" /> -->
<table class="a5" width="1050" align="center" style="font-family: Segoe UI; font-size: 14px;">
<?php
    $content = str_replace("<br />", "", file_get_contents("README.md"));
    $content = str_replace("# One-way labyrinth generator", "<span style=\"font-size: 18px; font-weight: bold\">One-way labyrinth generator</span>", $content);
    $pos = strpos($content, "---");
    $content = substr($content, 0, $pos);

    preg_match_all("|<!-- page \d+ -->|", $content, $matches);
    $startPos = 0;

    $output = "";

    if ($normalOrder) { // 1 2 3 4, for reading in browser
        for($i = 0; $i < count($matches[0]); $i++) {
            $pos = strpos($content, $matches[0][$i]);
    
            if ($i % 2 == 0) {
                $output.= "<tr><td class=\"left\"><div>".nl2br(trim(substr($content, $startPos, $pos - $startPos)))."<div></td>\n";
            }
            else {
                $output.= "<td class=\"right\"><div>".nl2br(trim(substr($content, $startPos, $pos - $startPos)))."<div></td></tr>";
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
                $pages[] = "<tr><td class=\"left\"><div>".nl2br(trim(substr($content, $startPos, $pos - $startPos)))."<div></td>\n";
            }
            else {
                $pages[] = "<td class=\"right\"><div>".nl2br(trim(substr($content, $startPos, $pos - $startPos)))."<div></td></tr>";
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
