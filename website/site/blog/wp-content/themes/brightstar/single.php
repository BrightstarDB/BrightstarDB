<?php get_header(); ?>

	<?php if (have_posts()) : while (have_posts()) : the_post(); ?>

		<article <?php post_class() ?> id="post-<?php the_ID(); ?>">
			<header>
				<time datetime="<?php echo date(DATE_W3C); ?>" pubdate class="updated"><?php the_time('F jS, Y') ?></time>
				<h1 class="entry-title"><?php the_title(); ?></h1>
			</header>

			<div class="entry-content">
				
				<?php the_content(); ?>

				<?php wp_link_pages(array('before' => 'Pages: ', 'next_or_number' => 'number')); ?>
				
				<?php include (TEMPLATEPATH . '/_/inc/meta.php' ); ?>

			</div>
			
			<br/><?php edit_post_link('Edit this entry','','.'); ?>
			
		</article>

		<nav class="prev-next-posts">
		    <ul>
				<li><?php previous_post('&laquo; &laquo; %', '', 'yes'); ?></li>
				<li><?php next_post('% &raquo; &raquo; ', '', 'yes'); ?></li>
			</ul>
		</nav>

<!--	< ? php comments_template(); ? > -->

	<?php endwhile; endif; ?>

<?php get_footer(); ?>