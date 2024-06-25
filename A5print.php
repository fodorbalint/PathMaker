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
    margin-left: 22.5px;  /* this affects the margin in the printer settings. For paper size A4, but the book format is 135 x 200 mm. */
    margin-right: 22.5px;  
    margin-top: 0px;
    margin-bottom: 0px;
    orientation: landscape;
}
</style>
<style>
    :root {
        --text: #black;
        --bg: #white;
        --border: white;
    }
    .a5 td { /* print */
        border: 0px solid #808080;  
        width: 470px;    
        height: 800px;  
        vertical-align: top;
        position: relative;
        padding-top: 30px;
        padding-bottom: 60px;
    }
    .a5_1 td { /* screen */
        border: 0px solid #808080;  
        width: 470px;    
        height: 800px;  
        vertical-align: top;
        position: relative;
        padding-top: 30px;
        padding-bottom: 60px;
    }
    .a5, .a5_1 {
        border-spacing: 0px;  
        margin-top: -8px;
    }
    @media (prefers-color-scheme: dark) {
        :root {
            --text: #fff;
            --bg: #000;
            --border: gray;
        }
    }
    body {
        background-color: var(--bg);
    }    
    .left {
        padding-left: 30px;
        padding-right: 40px;        
    }
    .right {
        padding-left: 40px;
        padding-right: 30px;
    }
    .left div {
        width: 100%;
        height: 710px;
        overflow-y: hidden;
        color: var(--text);
    }

    .right div {
        width: 100%;
        height: 710px;
        overflow-y: hidden;
        color: var(--text);
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
        text-align: center;
        left: 0px;
        bottom: 16px;
        width: 100%;
        height: 16px;
    }
    div.rightNum {
        position: absolute;
        left: 0px;
        bottom: 16px;
        width: 100%;
        height: 16px;
        text-align: center;
    }
    div.border {
        position: absolute;
        left: 0px;
        top: 0px;
        width: 100%;
        height: 100%;
        box-shadow: inset 0 0 0 1px var(--border);
    }
</style>
<table class="<?php $normalOrder == true ? print "a5_1" : print "a5" ?>" width="1080" align="center" style="font-family: Segoe UI; font-size: 14px;">
<?php
    $content = str_replace("\r", "", str_replace("<br />", "", file_get_contents("readme0.md")));
    
    $pos1 = strpos($content, "#");
    $pos2 = strpos($content, "\n",  $pos1);
    $header = substr($content, $pos1 + 2, $pos2 - $pos1 - 2);
    $content = "<span style=\"font-size: 18px; font-weight: bold\">$header</span>".substr($content, $pos2);

    $content = preg_replace_callback("/width=\"(\d+)\"/", function($matches) { return "width=\"".($matches[1]*100/21)."%\""; }, $content);

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
                $output.= "<tr><td class=\"left\"><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"leftNum\">".($i+1)."</div></td>\n";
            }            
            else {
                $output.= "<td class=\"right\"><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"rightNum\">".($i+1)."</div></td></tr>\n";
            }
        }
        if ($i % 2 == 1) {
            $output.= "<td class=\"right\"><div class=\"border\"></div></td></tr>\n";
        }
    }
    else { // 4 1 2 3, for printing on A4 paper on both sides
        $pages = array();
        
        for($i = 0; $i < count($pageTexts); $i++) {
            if ($i % 2 == 1) {
                if ($i % 4 == 1) {
                    $pages[] = "<tr><td class=\"left\"><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"leftNum\">".($i+1)."</div><div class=\"marking1\"></div><div class=\"marking2\"></div><div class=\"marking3\"></div><div class=\"marking4\"></div><div class=\"marking5\"></div><div class=\"marking6\"></div></td>\n";
                }
                else {
                    $pages[] = "<tr><td class=\"left\"><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"leftNum\">".($i+1)."</div></td>\n";
                }
            }
            else {
                $pages[] = "<td class=\"right\"><div class=\"border\"></div><div>".$pageTexts[$i]."</div><div class=\"rightNum\">".($i+1)."</div></td></tr>";
            }
        }
        if ($i % 4 == 1) {
            $pages[] = "<tr><td class=\"left\"><div class=\"border\"></div><div class=\"marking1\"></div><div class=\"marking2\"></div><div class=\"marking3\"></div><div class=\"marking4\"></div><div class=\"marking5\"></div><div class=\"marking6\"></div></td>\n";
            $pages[] = "<td class=\"right\"><div class=\"border\"></div></td></tr>\n";
            $pages[] = "<tr><td class=\"left\"><div class=\"border\"></div></td>\n";
        }
        else if ($i % 4 == 2) {
            $pages[] = "<td class=\"right\"><div class=\"border\"></div></td></tr>\n";
            $pages[] = "<tr><td class=\"left\"><div class=\"border\"></div></td>\n";
        }
        else if ($i % 4 == 3) {
            $pages[] = "<tr><td class=\"left\"><div class=\"border\"></div></td>\n";
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
