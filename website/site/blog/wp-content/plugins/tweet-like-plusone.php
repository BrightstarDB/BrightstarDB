<?php
/*
Plugin Name: Tweet, Like, Google +1 and Share
Plugin URI: http://letusbuzz.com/tweet-share-like-plusone
Author: Sudipto Pratap Mahato
Version: 1.2.1
Description: Most simple social share icons. 99% of your any blog post is share by these Social share icons.
Requires at least: 3.0
Tested up to: 3.2.1
Stable tag: 1.2.1
*/

function disp_social($content) {
global $post;
if (get_option('s4dmob', false )==true && social4i_check_mobile())return $content;
$plink = get_permalink($post->ID);
$eplink = urlencode($plink);
$ptitle = get_the_title($post->ID);
$disps4=0;
$abvcnt=0;
$belcnt=0;
$expostid=str_replace(' ','',get_option('s4excludeid',''));
$expostcat=str_replace(' ','',get_option('s4excludecat',''));
$clang=get_option( 's4fblikelang', 'en_US' );
if($expostid!=''){
	$pids=explode(",",$expostid);
	if (in_array($post->ID, $pids)) {
    		return $content;
	}
}
if($expostcat!=''){
	$pcat=explode(",",$expostcat);
	if (in_category($pcat)) {
    		return $content;
	}
}
$twsc='<script type="text/javascript" src="http://platform.twitter.com/widgets.js"></script>';
$flsc='<script type="text/javascript" src="http://connect.facebook.net/'.$clang.'/all.js#xfbml=1"></script>';
$gpsc='<script type="text/javascript" src="http://apis.google.com/js/plusone.js"></script>';
$fssc='<script src="http://static.ak.fbcdn.net/connect.php/js/FB.Share" type="text/javascript"></script>';
if (get_option( 's4optimize', true ) == true){
$twsc='';
$flsc='';
$gpsc='';
$fssc='';
}
if(is_single()&&get_option( 's4onpost', true ) == true){
	$disps4=1;
	if (get_option( 's4pabovepost', true ) == true)$abvcnt=1;
	if (get_option( 's4pbelowpost', false ) == true)$belcnt=1;
}
if(is_page()&&get_option( 's4onpage', true ) == true){
	$disps4=1;
	if (get_option( 's4pgabovepost', true ) == true)$abvcnt=1;
	if (get_option( 's4pgbelowpost', false ) == true)$belcnt=1;
}
if(is_home()&&get_option( 's4onhome', false ) == true){
	$disps4=1;
	if (get_option( 's4habovepost', true ) == true)$abvcnt=1;
	if (get_option( 's4hbelowpost', false ) == true)$belcnt=1;
}
if((is_archive()||is_search())&&get_option( 's4onarchi', false ) == true){
	$disps4=1;
	if (get_option( 's4aabovepost', true ) == true)$abvcnt=1;
	if (get_option( 's4abelowpost', false ) == true)$belcnt=1;
}

if ($disps4==1){
	$size=get_option( 's4iconsize', 'large' );
	$align=get_option( 's4iconalign', 'left' );
	if($align=="left")$align="align-left";
	if($align=="right")$align="align-right";
	if($align=="floatl")$align="float-left";
	if($align=="floatr")$align="float-right";
	$sharelinks=display_social4i($size,$align);
	if ($abvcnt==1)$content=$sharelinks.$content;
	if ($belcnt==1)$content=$content.$sharelinks;
}
return $content;
}
function s4load_script()
{
	$clang=get_option( 's4fblikelang', 'en_US' );
	$r='';
	if(get_option('s4allscripts',true)== true){
	$r='<script type="text/javascript" src="http://platform.twitter.com/widgets.js"></script><script type="text/javascript" src="http://connect.facebook.net/'.$clang.'/all.js#xfbml=1"></script><script type="text/javascript" src="http://apis.google.com/js/plusone.js"></script><script src="http://static.ak.fbcdn.net/connect.php/js/FB.Share" type="text/javascript"></script><script type="text/javascript" src="http://platform.linkedin.com/in.js"></script>';
	}
	else
	{
			if(get_option('s4_twitter','1'))$r.='<script type="text/javascript" src="http://platform.twitter.com/widgets.js"></script>';
			if(get_option('s4_fblike','1'))$r.='<script type="text/javascript" src="http://connect.facebook.net/'.$clang.'/all.js#xfbml=1"></script>';
			if(get_option('s4_plusone','1'))$r.='<script type="text/javascript" src="http://apis.google.com/js/plusone.js"></script>';
			if(get_option('s4_linkedin',false))$r.='<script type="text/javascript" src="http://platform.linkedin.com/in.js"></script>';
			if(get_option('s4_fbshare','1'))$r.='<script src="http://static.ak.fbcdn.net/connect.php/js/FB.Share" type="text/javascript"></script>';
			
	}
	return $r;
}

function social4i_css() {
s4_fb_share_thumb();
if (get_option('s4dmob', false )==true && social4i_check_mobile())return;
echo '<style type="text/css">div.socialicons{float:left;display:block;margin-right: 10px;}div.socialicons p{margin-bottom: 0px !important;margin-top: 0px !important;padding-bottom: 0px !important;padding-top: 0px !important;}</style>'."\n";
if(get_option('s4optimize',true)==true&&get_option( 's4scripthead', 'head' ) == "head" )
echo s4load_script();
$ccss=get_option('s4ccss','');
if(trim($ccss!=''))echo '<style type="text/css">'.$ccss.'</style>';

}
function social4i_foot()
{
if (get_option('s4dmob', false )==true && social4i_check_mobile())return;
	if(get_option('s4optimize',true)==true&&get_option( 's4scripthead', 'head' ) == "foot" )
		echo s4load_script();
}
function s4_fb_share_thumb()
{
$thumb = false;
if(function_exists('get_post_thumbnail_id')&&function_exists('wp_get_attachment_image_src'))
{
	$image_id = get_post_thumbnail_id();
	$image_url = wp_get_attachment_image_src($image_id,'large');
	$thumb = $image_url[0]; 
}
$default_img = get_option('s4defthumb',''); 
if ( $thumb == false ) 
	$thumb=$default_img; 

if(is_single() || is_page()) { 
?>
	<meta property="og:type" content="article" />
	<meta property="og:title" content="<?php single_post_title(''); ?>" />
	<meta property="og:url" content="<?php the_permalink(); ?>"/>
	<?php if(trim($thumb)!=''){ ?>
		<meta property="og:image" content="<?php echo $thumb; ?>" />
	<?php } ?>
<?php  } else { ?>
	<meta property="og:type" content="article" />
  	<meta property="og:title" content="<?php bloginfo('name'); ?>" />
	<meta property="og:url" content="<?php bloginfo('url'); ?>"/>
	<meta property="og:description" content="<?php bloginfo('description'); ?>" />
	<?php if(trim($default_img)!=''){ ?>
		<meta property="og:image" content="<?php echo $default_img; ?>" />
	<?php } ?>
<?php  } 

}

function disp_social_on_optionpage()
{
$plink = "http://wordpress.org/extend/plugins/only-tweet-like-share-and-google-1/";
$eplink = urlencode($plink);
$ptitle = "Check out this cool Social Share Plugin for Wordpress";
$sharelinks='<div id="social4i" style="position: relative; display: block;">';
$clang=get_option( 's4fblikelang', 'en_US' ); 
if(get_option('s4_twitter','1')){
if (get_option( 's4iconsize', 'large' ) == "large" )$tp="vertical"; else $tp="horizontal";
$sharelinks.= '<div class=socialicons style="float:left;margin-right: 10px;"><a href="http://twitter.com/share" data-url="'.$plink.'" data-counturl="'.$plink.'" data-text="'.$ptitle.'" class="twitter-share-button" data-count="'.$tp.'">Tweet</a><script type="text/javascript" src="http://platform.twitter.com/widgets.js"></script></div>';
}
if(get_option('s4_fblike','1')){
if(get_option('s4_fbsend',false)==true)$snd="true"; else $snd="false";
if (get_option( 's4iconsize', 'large' ) == "large" )
	$tp=' layout="box_count" width="55" height="62" ';
else 
	$tp=' layout="button_count" width="100" height="21" ';
	
$sharelinks.= '<div class=socialicons style="float:left;margin-right: 10px;"><div id="fb-root"></div><script src="http://connect.facebook.net/'.$clang.'/all.js#xfbml=1"></script><fb:like href="'.$eplink.'" send="'.$snd.'"'.$tp.'show_faces="false" font=""></fb:like></div>';
}
if(get_option('s4_plusone','1')){
if (get_option( 's4iconsize', 'large' ) == "large" )$tp="tall"; else $tp="medium";
$sharelinks.='<div class="socialicons" style="float:left;margin-right: 10px;"><script type="text/javascript" src="http://apis.google.com/js/plusone.js"></script><g:plusone size="'.$tp.'" href="'.$eplink.'" count="true"></g:plusone></div>';
}
if(get_option( 's4_linkedin', false )){
if (get_option( 's4iconsize', 'large' ) == "large" )$tp="top"; else $tp="right";
$sharelinks.='<div class="socialicons" style="float:left;margin-right: 10px;"><script type="text/javascript" src="http://platform.linkedin.com/in.js"></script><script type="in/share" data-url="'.$eplink.'" data-counter="'.$tp.'"></script></div>';
}
if(get_option('s4_fbshare','1')){
if (get_option( 's4iconsize', 'large' ) == "large" )
{
	$tp="box_count";
	$cs1="height:60px;";
	$cs2='style="position: absolute; bottom: 0pt;"';
} else $tp="button_count";
$sharelinks.= '<div class=socialicons style="position: relative;'.$cs1.'float:left;margin-right: 10px;"><div '.$cs2.'><a name="fb_share" type="'.$tp.'" share_url="'.$eplink.'" href="http://www.facebook.com/sharer.php">Share</a><script src="http://static.ak.fbcdn.net/connect.php/js/FB.Share" type="text/javascript"></script></div></div>';
}
$sharelinks.= '<div style="clear:both"></div></div>';
echo $sharelinks;
}
function social4ioptions(){
?>
<h2>Tweet, Like, Share and Google +1 Option Page</h2>
Like this Plugin then why not hit the like button. Your like will motivate me to enhance the features of the Plugin :)<br />
<iframe style="overflow: hidden; width: 450px; height: 35px;" src="http://www.facebook.com/plugins/like.php?app_id=199883273397074&amp;href=http%3A%2F%2Fwww.facebook.com%2Fpages%2FTech-XT%2F223482634358279&amp;send=false&amp;layout=standard&amp;width=450&amp;show_faces=false&amp;action=like&amp;colorscheme=light&amp;font&amp;height=35" frameborder="0" scrolling="no" width="320" height="35"></iframe><br />And if you are too generous then you can always <b>DONATE</b> by clicking the donation button.<br/>If you like the plugin then <b>write a review</b> of it pointing out the plus and minus points.<br /><a href="http://letusbuzz.com/tweet-share-like-plusone" TARGET='_blank'>Click here</a> for <b>Reference on using shortcode/Function</b> or if you want to <b>report a bug</b>. 
<table class="form-ta">	
<tr valign="top">
<td width="78%">
<form method="post" action="options.php">
<h3>Test Buttons</h3>
<?php disp_social_on_optionpage(); ?>

<h3 style="color: #cc0000;">Increase Page Load Speed</h3>
<p>Note: After using this option if the buttons do not get displayed properly then uncheck it</p>
<p><input type="checkbox" name="s4optimize" id="s4optimize" value="true"<?php if (get_option( 's4optimize', true ) == true) echo ' checked'; ?>>Optimize the script for faster loading</p>

&nbsp;&nbsp;&nbsp;&nbsp;<input type="radio" name="s4scripthead" value="head" id="s4scripthead1"<?php if (get_option( 's4scripthead', 'head' ) == "head" ) echo ' checked'; ?>></input><label for="s4scripthead">Place Script in the Header&nbsp;&nbsp;&nbsp;&nbsp;</label>
<input type="radio" name="s4scripthead" value="foot" id="s4scripthead2"<?php if (get_option( 's4scripthead', 'head' ) == "foot" ) echo ' checked'; ?>></input><label for="s4scripthead">Place Script in the Footer</label>

<p>Keep this option checked if you are using Shortcode or PHP function to display the buttons<br/><input type="checkbox" name="s4allscripts" id="s4allscripts" value="true"<?php if (get_option( 's4allscripts', true ) == true) echo ' checked'; ?>>Load all scripts</p>

<h3 style="color: #cc0000;">Select Icons to display</h3>
<p><input type="checkbox" name="s4_twitter" id="s4-twitter" value="true"<?php if (get_option( 's4_twitter', true ) == true) echo ' checked'; ?>> Display Twitter&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;via @<input type="text" name="s4twtvia" style="width: 150px;" value="<?php echo get_option('s4twtvia',''); ?>" /></p>
<p><input type="checkbox" name="s4_fblike" id="s4-fblike" value="true"<?php if (get_option( 's4_fblike', true ) == true) echo ' checked'; ?>> Display Facebook Like&nbsp;&nbsp;&nbsp;&nbsp;<input type="checkbox" name="s4_fbsend" id="s4-fbsend" value="true"<?php if (get_option( 's4_fbsend', false ) == true) echo ' checked'; ?>> Display Facebook Send </p>
<p>&nbsp;&nbsp;&nbsp;&nbsp;Select Facebook Like Language <?php s4_lang_disp(); ?> </p>
<p><input type="checkbox" name="s4_plusone" id="s4-plusone" value="true"<?php if (get_option( 's4_plusone', true ) == true) echo ' checked'; ?>> Display Google +1 </p>
<p><input type="checkbox" name="s4_fbshare" id="s4-fbshare" value="true"<?php if (get_option( 's4_fbshare', true ) == true) echo ' checked'; ?>> Display Facebook Share </p>
<p><input type="checkbox" name="s4_linkedin" id="s4_linkedin" value="true"<?php if (get_option( 's4_linkedin', false ) == true) echo ' checked'; ?>> Display Linkedin </p>
<p><b>Default Thumbnail URL</b> <input type="text" name="s4defthumb" style="width: 300px;" value="<?php echo get_option('s4defthumb',''); ?>" /></p>

<h3 style="color: #cc0000;">Size of Icons</h3>
<input type="radio" name="s4iconsize" value="large" id="s4iconsize1"<?php if (get_option( 's4iconsize', 'large' ) == "large" ) echo ' checked'; ?>></input><label for="s4iconsize">Large&nbsp;&nbsp;&nbsp;&nbsp;</label>
<input type="radio" name="s4iconsize" value="small" id="s4iconsize2"<?php if (get_option( 's4iconsize', 'large' ) == "small" ) echo ' checked'; ?>></input><label for="s4iconsize">Small</label>

<h3 style="color: #cc0000;">Alignment</h3>
<input type="radio" name="s4iconalign" value="left" id="s4iconalign1"<?php if (get_option( 's4iconalign', 'left' ) == "left" ) echo ' checked'; ?>></input><label for="s4iconsize">Left Aligned&nbsp;&nbsp;&nbsp;&nbsp;</label>
<input type="radio" name="s4iconalign" value="right" id="s4iconalign2"<?php if (get_option( 's4iconalign', 'left' ) == "right" ) echo ' checked'; ?>></input><label for="s4iconsize">Right Aligned&nbsp;&nbsp;&nbsp;&nbsp;</label>
<input type="radio" name="s4iconalign" value="floatl" id="s4iconalign3"<?php if (get_option( 's4iconalign', 'left' ) == "floatl" ) echo ' checked'; ?>></input><label for="s4iconsize">Float Left&nbsp;&nbsp;&nbsp;&nbsp;</label>
<input type="radio" name="s4iconalign" value="floatr" id="s4iconalign3"<?php if (get_option( 's4iconalign', 'left' ) == "floatr" ) echo ' checked'; ?>></input><label for="s4iconsize">Float Right&nbsp;&nbsp;&nbsp;&nbsp;</label>
		
<h3 style="color: #cc0000;">Where to Display</h3>
<p><input type="checkbox" name="s4onpost" id="s4onpost" value="true"<?php if (get_option( 's4onpost', true ) == true) echo ' checked'; ?>> <b>Display on Posts</b> </p>
<div style="margin-left: 30px;">
<p><input type="checkbox" name="s4pabovepost" id="s4abovepost" value="true"<?php if (get_option( 's4pabovepost', true ) == true) echo ' checked'; ?>> Display Above Content </p>
<p><input type="checkbox" name="s4pbelowpost" id="s4belowpost" value="true"<?php if (get_option( 's4pbelowpost', false ) == true) echo ' checked'; ?>>Display Below Content</p>
</div>
<p><input type="checkbox" name="s4onpage" id="s4onpage" value="true"<?php if (get_option( 's4onpage', true ) == true) echo ' checked'; ?>><b>Display on Pages</b></p>
<div style="margin-left: 30px;">
<p><input type="checkbox" name="s4pgabovepost" id="s4abovepost" value="true"<?php if (get_option( 's4pgabovepost', true ) == true) echo ' checked'; ?>> Display Above Content </p>
<p><input type="checkbox" name="s4pgbelowpost" id="s4belowpost" value="true"<?php if (get_option( 's4pgbelowpost', false ) == true) echo ' checked'; ?>>Display Below Content</p>
</div>
<p><input type="checkbox" name="s4onhome" id="s4onhome" value="true"<?php if (get_option( 's4onhome', false ) == true) echo ' checked'; ?>><b>Display on Home Page</b> </p>
<div style="margin-left: 30px;">
<p><input type="checkbox" name="s4habovepost" id="s4abovepost" value="true"<?php if (get_option( 's4habovepost', true ) == true) echo ' checked'; ?>> Display Above Content </p>
<p><input type="checkbox" name="s4hbelowpost" id="s4belowpost" value="true"<?php if (get_option( 's4hbelowpost', false ) == true) echo ' checked'; ?>>Display Below Content</p>
</div>
<p><input type="checkbox" name="s4onarchi" id="s4onarchi" value="true"<?php if (get_option( 's4onarchi', false ) == true) echo ' checked'; ?>><b>Display on Archive Pages(Categories, Tages, Author etc.)</b></p>
<div style="margin-left: 30px;">
<p><input type="checkbox" name="s4aabovepost" id="s4abovepost" value="true"<?php if (get_option( 's4aabovepost', true ) == true) echo ' checked'; ?>> Display Above Content </p>
<p><input type="checkbox" name="s4abelowpost" id="s4belowpost" value="true"<?php if (get_option( 's4abelowpost', false ) == true) echo ' checked'; ?>>Display Below Content</p>
</div>
<p><input type="checkbox" name="s4onexcer" id="s4onexcer" value="true"<?php if (get_option( 's4onexcer', true ) == true) echo ' checked'; ?>><b>Display on Excerpts</b></p>

<h3 style="color: #cc0000;">Mobile browsers</h3>
<p><input type="checkbox" name="s4dmob" id="s4dmob" value="true"<?php if (get_option( 's4dmob', false ) == true) echo ' checked'; ?>><b>Disable on Mobile Browser</b><br /> Check this option if you have installed a mobile theme plugin like Wptouch, WordPress Mobile Pack etc.</p>

<h3 style="color: #cc0000;">Don't display on Posts/Pages</h3>
<p>Enter the ID's of those Pages/Posts separated by comma. e.g 13,5,87<br/>
<input type="text" name="s4excludeid" style="width: 300px;" value="<?php echo get_option('s4excludeid',''); ?>" /></p>

<h3 style="color: #cc0000;">Don't display on Category</h3>
<p>Enter the ID's of those Categories separated by comma. e.g 131,45,817<br/>
<input type="text" name="s4excludecat" style="width: 300px;" value="<?php echo get_option('s4excludecat',''); ?>" /></p>

<h3 style="color: #cc0000;">Insert Custom CSS</h3>
<small>Your theme should have Call to wp_head() function</small><br />
<p><textarea name="s4ccss" rows="10" cols="50" style="width:600px;"><?php echo stripslashes(htmlspecialchars(get_option('s4ccss',''))); ?></textarea></p>

<div style="clear:both"></div>
<input type="submit" class="button-primary" value="Save Changes"/>
<?php wp_nonce_field('update-options'); ?>
<input type="hidden" name="page_options" value="s4pabovepost,s4pbelowpost,s4pgabovepost,s4pgbelowpost,s4habovepost,s4hbelowpost,s4aabovepost,s4abelowpost,s4_twitter,s4_fblike,s4_plusone,s4_fbshare,s4onpost,s4onpage,s4onhome,s4onarchi,s4iconsize,s4iconalign,s4excludeid,s4_fbsend,s4optimize,s4twtvia,s4excludecat,s4defthumb,s4onexcer,s4fblikelang,s4ccss,s4_linkedin,s4scripthead,s4allscripts,s4dmob">
<input type="hidden" name="action" value="update" />
</form>
</td><td width="2%">&nbsp;</td><td width="20%"><b>Follow us on</b><br/><a href="http://twitter.com/letusbuzz" target="_blank"><img src="http://a0.twimg.com/a/1303316982/images/twitter_logo_header.png" /></a><br/><a href="http://facebook.com/letusbuzzz" target="_blank"><img src="https://secure-media-sf2p.facebook.com/ads3/creative/pressroom/jpg/b_1234209334_facebook_logo.jpg" height="38px" width="118px"/></a><p></p><b>Feeds and News</b><br /><?php get_feeds_s4() ?>
<p></p>
<form action="https://www.paypal.com/cgi-bin/webscr" method="post">
<input type="hidden" name="cmd" value="_donations">
<input type="hidden" name="business" value="isudipto@gmail.com">
<input type="hidden" name="lc" value="US">
<input type="hidden" name="item_name" value="Tweet Like Share Plusone Plugin">
<input type="hidden" name="no_note" value="0">
<input type="hidden" name="currency_code" value="USD">
<input type="hidden" name="bn" value="PP-DonationsBF:btn_donateCC_LG.gif:NonHostedGuest">
<input type="image" src="https://www.paypalobjects.com/WEBSCR-640-20110401-1/en_US/i/btn/btn_donateCC_LG.gif" border="0" name="submit" alt="PayPal - The safer, easier way to pay online!">
<img alt="" border="0" src="https://www.paypalobjects.com/WEBSCR-640-20110401-1/en_US/i/scr/pixel.gif" width="1" height="1">
</form>
<br />Consider a Donation and remember $X is always better than $0
</td></tr></table>
<?php
}

add_action('wp_footer', 'social4i_foot');
add_action('wp_head', 'social4i_css');
add_filter('the_content', 'disp_social',1);
if (get_option( 's4onexcer', true ) == true)
add_filter('the_excerpt', 'disp_social');
add_action('admin_menu', 'socialicons_addmenu');

function get_feeds_s4() {
	include_once(ABSPATH . WPINC . '/feed.php');
	$rss = fetch_feed('http://feeds.feedburner.com/letusbuzz');
	if (!is_wp_error( $rss ) ){
		$rss5 = $rss->get_item_quantity(5); 
		$rss1 = $rss->get_items(0, $rss5); 
	}
?>
<ul>
<?php if (!$rss5 == 0)foreach ( $rss1 as $item ){?>
<li style="list-style-type:circle">
<a target="_blank" href='<?php echo $item->get_permalink(); ?>'><?php echo $item->get_title(); ?></a>
</li>
<?php } ?>
</ul>
<?php
}
function socialicons_addmenu(){
	add_options_page("Tweet Like Share Plusone", "Tweet Like Plusone", "administrator", "social4i", "social4ioptions");
}
//===================================================================================//
function display_social4i($size,$align, $type = FALSE)
{
global $post;
if($size=='')$size="large";
if($align=='')$align="align-left";
$plink = get_permalink($post->ID);
$eplink = urlencode($plink);
$ptitle = get_the_title($post->ID);
$eptitle=str_replace(array(">","<"),"",$ptitle);
$via=get_option('s4twtvia','');
$clang=get_option( 's4fblikelang', 'en_US' );
$twsc='<script type="text/javascript" src="http://platform.twitter.com/widgets.js"></script>';
$flsc='<script type="text/javascript" src="http://connect.facebook.net/'.$clang.'/all.js#xfbml=1"></script>';
$gpsc='<script type="text/javascript" src="http://apis.google.com/js/plusone.js"></script>';
$fssc='<script src="http://static.ak.fbcdn.net/connect.php/js/FB.Share" type="text/javascript"></script>';
$lnsc='<script type="text/javascript" src="http://platform.linkedin.com/in.js"></script>';
if (get_option( 's4optimize', true ) == true){
$twsc='';
$flsc='';
$gpsc='';
$fssc='';
$lnsc='';
}

if ($size == "large" ){
	if(get_option('s4_fbsend',false)==true)
		$css1="height:82px;"; 
	else 
		$css1="height:69px;";
}
else $css1="height:29px;";
$css2=$css1;
if ($align == "float-right" ){$css2.="float: right;";$css1.="float: right;";}
if ($align == "float-left" ){$css2.="float: left;";$css1.="float: left;";}
if ($align == "align-left" )$css1.="float: left;";
if ($align == "align-right" )$css1.="float: right;";
$sharelinks='<div class="social4i" style="'.$css2.'"><div class="social4in" style="'.$css1.'">';
if(get_option('s4_twitter','1') && $type === FALSE || $type == "s4_twitter"){
if ($size == "large" )$tp="vertical"; else $tp="horizontal";
$sharelinks.= '<div class="socialicons s4twitter" style="float:left;margin-right: 10px;"><a href="http://twitter.com/share" data-url="'.$plink.'" data-counturl="'.$plink.'" data-text="'.$eptitle.'" class="twitter-share-button" data-count="'.$tp.'" data-via="'.$via.'"></a>'.$twsc.'</div>';
}
if(get_option('s4_fblike','1') && $type === FALSE || $type == "s4_fblike" || $type == "s4_fbsend"){
if(get_option('s4_fbsend',false)==true || $type == "s4_fbsend")$snd="true"; else $snd="false";
if ($size == "large" )
	$tp=' layout="box_count" width="55" height="62" ';
else 
	$tp=' layout="button_count" width="100" height="21" ';
	
$sharelinks.= '<div class="socialicons s4fblike" style="float:left;margin-right: 10px;"><div id="fb-root"></div>'.$flsc.'<fb:like href="'.$eplink.'" send="'.$snd.'"'.$tp.'show_faces="false" font=""></fb:like></div>';
}
if(get_option('s4_plusone','1') && $type === FALSE || $type == "s4_plusone"){
if ($size == "large" )$tp="tall"; else $tp="medium";
$sharelinks.='<div class="socialicons s4plusone" style="float:left;margin-right: 10px;">'.$gpsc.'<g:plusone size="'.$tp.'" href="'.$plink.'" count="true"></g:plusone></div>';
}
if(get_option( 's4_linkedin', false )&& $type === FALSE || $type == "s4_linkedin"){
if ($size == "large" )$tp="top"; else $tp="right";
$sharelinks.='<div class="socialicons s4linkedin" style="float:left;margin-right: 10px;">'.$lnsc.'<script type="in/share" data-url="'.$plink.'" data-counter="'.$tp.'"></script></div>';
}
if(get_option('s4_fbshare','1') && $type === FALSE || $type == "s4_fbshare"){
if ($size == "large" )
{
	$tp="box_count";
	$cs1="height: 61px;width:61px;background:url(https://lh6.googleusercontent.com/-khBs3Dennc8/TiggtecoVQI/AAAAAAAAAPM/fiINPv9guK4/fbshare.png) no-repeat;";
	$cs2='style="position: absolute; bottom: 0pt;"';
} else $tp="button_count";
$sharelinks.= '<div class="socialicons s4fbshare" style="position: relative;'.$cs1.'float:left;margin-right: 10px;"><div class="s4ifbshare" '.$cs2.'><a name="fb_share" type="'.$tp.'" share_url="'.$eplink.'" href="http://www.facebook.com/sharer.php"></a>'.$fssc.'</div></div>';
}
$sharelinks.= '</div><div style="clear:both"></div></div>';
return $sharelinks;
}

//Geilt - Alexander Conroy geilt@esotech.org http://www.esotech.org and http://www.geilt.com 
//Added: $type: 
//s4_plusone s4_fbshare,s4_fblike, s4_twitter, s4_fbsend
function social4i_shortcode($atts){
	extract(shortcode_atts( array('size' => 'large','align'=>'align-left', 'type' => FALSE), $atts ));
	$ss=display_social4i($size,$align, $type);
	return $ss;
}
add_shortcode( 'social4i', 'social4i_shortcode' );
function s4_lang_disp()
{
$alllang=array("Catalan|ca_ES","Czech|cs_CZ","Welsh|cy_GB","Danish|da_DK","German|de_DE","Basque|eu_ES","English (Pirate)|en_PI","English (Upside Down)|en_UD","Cherokee|ck_US","English (US)|en_US","Spanish|es_LA","Spanish (Chile)|es_CL","Spanish (Colombia)|es_CO","Spanish (Spain)|es_ES","Spanish (Mexico)|es_MX","Spanish (Venezuela)|es_VE","Finnish (test)|fb_FI","Finnish|fi_FI","French (France)|fr_FR","Galician|gl_ES","Hungarian|hu_HU","Italian|it_IT","Japanese|ja_JP","Korean|ko_KR","Norwegian (bokmal)|nb_NO","Norwegian (nynorsk)|nn_NO","Dutch|nl_NL","Polish|pl_PL","Portuguese (Brazil)|pt_BR","Portuguese (Portugal)|pt_PT","Romanian|ro_RO","Russian|ru_RU","Slovak|sk_SK","Slovenian|sl_SI","Swedish|sv_SE","Thai|th_TH","Turkish|tr_TR","Kurdish|ku_TR","Simplified Chinese (China)|zh_CN","Traditional Chinese (Hong Kong)|zh_HK","Traditional Chinese (Taiwan)|zh_TW","Leet Speak|fb_LT","Afrikaans|af_ZA","Albanian|sq_AL","Armenian|hy_AM","Azeri|az_AZ","Belarusian|be_BY","Bengali|bn_IN","Bosnian|bs_BA","Bulgarian|bg_BG","Croatian|hr_HR","Dutch (Belgie)|nl_BE","English (UK)|en_GB","Esperanto|eo_EO","Estonian|et_EE","Faroese|fo_FO","French (Canada)|fr_CA","Georgian|ka_GE","Greek|el_GR","Gujarati|gu_IN","Hindi|hi_IN","Icelandic|is_IS","Indonesian|id_ID","Irish|ga_IE","Javanese|jv_ID","Kannada|kn_IN","Kazakh|kk_KZ","Latin|la_VA","Latvian|lv_LV","Limburgish|li_NL","Lithuanian|lt_LT","Macedonian|mk_MK","Malagasy|mg_MG","Malay|ms_MY","Maltese|mt_MT","Marathi|mr_IN","Mongolian|mn_MN","Nepali|ne_NP","Punjabi|pa_IN","Romansh|rm_CH","Sanskrit|sa_IN","Serbian|sr_RS","Somali|so_SO","Swahili|sw_KE","Filipino|tl_PH","Tamil|ta_IN","Tatar|tt_RU","Telugu|te_IN","Malayalam|ml_IN","Ukrainian|uk_UA","Uzbek|uz_UZ","Vietnamese|vi_VN","Xhosa|xh_ZA","Zulu|zu_ZA","Khmer|km_KH","Tajik|tg_TJ","Arabic|ar_AR","Hebrew|he_IL","Urdu|ur_PK","Persian|fa_IR","Syriac|sy_SY","Yiddish|yi_DE","Guarani|gn_PY","Quechua|qu_PE","Aymara|ay_BO","Northern Sami|se_NO","Pashto|ps_AF","Klingon|tl_ST");
echo '<select name="s4fblikelang">';
$clang=get_option( 's4fblikelang', 'en_US' );
foreach($alllang as $lang)
{
	$l1=explode("|",$lang);
	if($l1[1]==$clang)$l2=' selected="selected"';else $l2='';
	echo '<option value="'.$l1[1].'"'.$l2.'>'.$l1[0].'</option>';
}
echo '</select>';
}
function social4i_check_mobile()
{
//This mobile browser check code is taken from Mobilepress plugin
$ismob=false;
switch(TRUE)
{	
	case (preg_match('/(apple|iphone|ipod)/i', $_SERVER['HTTP_USER_AGENT']) && preg_match('/mobile/i', $_SERVER['HTTP_USER_AGENT'])):
		$ismob="true";
		break; 
	case (preg_match('/(blackberry|configuration\/cldc|hp |hp-|htc |htc_|htc-|iemobile|kindle|midp|mmp|motorola|mobile|nokia|opera mini|opera mobi|palm|palmos|pocket|portalmmm|ppc;|smartphone|sonyericsson|sqh|spv|symbian|treo|up.browser|up.link|vodafone|windows ce|xda |xda_)/i', $_SERVER['HTTP_USER_AGENT'])):
		$ismob=true;
		break; 
	case (((strpos(strtolower($_SERVER['HTTP_ACCEPT']),'text/vnd.wap.wml') > 0) || (strpos(strtolower($_SERVER['HTTP_ACCEPT']),'application/vnd.wap.xhtml+xml')>0)) || ((isset($_SERVER['HTTP_X_WAP_PROFILE']) || isset($_SERVER['HTTP_PROFILE'])))):
		$ismob=true;
		break; 
	case (in_array(strtolower(substr($_SERVER['HTTP_USER_AGENT'],0,3)),array('lg '=>'lg ','lg-'=>'lg-','lg_'=>'lg_','lge'=>'lge'))):
		$ismob=true;
		break; 
	case (in_array(strtolower(substr($_SERVER['HTTP_USER_AGENT'],0,4)),array('acs-'=>'acs-','amoi'=>'amoi','doco'=>'doco','eric'=>'eric','huaw'=>'huaw','lct_'=>'lct_','leno'=>'leno','mobi'=>'mobi','mot-'=>'mot-','moto'=>'moto','nec-'=>'nec-','phil'=>'phil','sams'=>'sams','sch-'=>'sch-','shar'=>'shar','sie-'=>'sie-','wap_'=>'wap_','zte-'=>'zte-'))):
		$ismob=true;
		break;
	case (preg_match('/Googlebot-Mobile/i', $_SERVER['HTTP_USER_AGENT']) || preg_match('/YahooSeeker\/M1A1-R2D2/i', $_SERVER['HTTP_USER_AGENT'])):
		$ismob=true;
		break;
}
return $ismob;
}
?>