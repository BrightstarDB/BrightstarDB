<footer id="footer">
	<nav>
            <h1>
                Navigation</h1>
            <ul>
                <li><a href="/documentation/">Documentation</a></li>
                <li><a href="/download/">Download</a></li>
                <li><a href="/service/">Service</a></li>
                <li><a href="/blog/" class="current">Blog</a></li>
                <li><a href="/community/">Community</a></li>
                <li><a href="/about/">About / Contact</a></li>
            </ul>
        </nav>
        <nav>
            <h1>
                Keep in Touch!</h1>
            <p>
                <a href="http://twitter.com/brightstardb" class="twitter-follow-button" data-show-count="false">
                    Follow @BrightstarDB</a></p>
            <script src="http://platform.twitter.com/widgets.js" type="text/javascript"></script>
            <div data-href="http://brightstardb.com/" class="g-plusone" data-size="medium">
            </div>
            <p>
                <img src="/images/feed-icon16x16.png" alt="RSS Feed Icon" style="vertical-align: middle" />
                <a href="/blog/?feed=rss2">Blog RSS Feed</a>
            </p>
        </nav>
        <p>
            &copy; BrightstarDB Limited 2012</p>
        <p>
            Registered in England and Wales no. 7992038. Registered Office: 17 Cosgrove Road,
            Old Stratford, Milton Keynes, MK19 6AG</p>

	<?php wp_footer(); ?>
</footer>


<!-- here comes the javascript -->

<!-- jQuery is called via the Wordpress-friendly way via functions.php -->

<!-- this is where we put our custom functions -->
<script src="<?php bloginfo('template_directory'); ?>/_/js/functions.js"></script>

<script type="text/javascript" src="https://apis.google.com/js/plusone.js">
  {lang: 'en-GB'}
</script>

<script type="text/javascript">

  var _gaq = _gaq || [];
  _gaq.push(['_setAccount', 'UA-25503555-1']);
  _gaq.push(['_trackPageview']);

  (function() {
    var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
    ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
    var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
  })();

</script>

</body>

</html>
