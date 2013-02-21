<?php
/*
Template Name: Archive
*/
?>

<?php get_header(); ?>

<?php
$debut = 0; //The first article to be displayed
?>
<?php while(have_posts()) : the_post(); ?>
<article class="archive">
  <header>
    <h1>
      <?php the_title(); ?>
    </h1>
  </header>
  <ul class="archive">
    <?php $myposts = get_posts('numberposts=-1&offset=$debut');
       foreach($myposts as $post) : ?>
        <li>
          <time datetime="<?php echo date(DATE_W3C); ?>" pubdate class="updated"><?php the_time('F jS, Y') ?></time>    
          <a href="<?php the_permalink(); ?>"><?php the_title(); ?>
          </a>
          
        </li>
    <?php endforeach; ?>
  </ul>
</article>
<?php endwhile; ?>


  <?php get_footer(); ?>