// images contained in this folder were created by:
// 1) downloading a black screen vid from youtube in 640x360 mp4 format
// 2) generated images using ffmpeg, command:
//    ffmpeg -i explosion.mp4 -r 5 -s 320x180 explosion2\explosion%03d.png
// 3) also created an edgeMask image to mask out the edges when rendering

// 4) explosionSEQ.jpg image was created using SequenceImagePacker, args:
// Data\MaterialEffects\Textures\explosion2 -sp explosion -sx png -ss 1 -se 100 -sf 03 -ox 60 -outx jpg

// the original images I got from 2) was 320x180 but the image was not centered. and this is why the
// -ox 60 offset option was used for SequenceImagePacker. Each resulting image then was sized down to
// 260x180, which then got packed into a single 10x10 frame image file resulted in 2600x1800 size.
