// images contained in this folder were created by:
// 1) downloading a black screen vid from youtube in 640x360 mp4 format
// 2) generated images using ffmpeg, command:
//    ffmpeg -i torch.mp4 -r 20 torch3\torch%03d.png
// 3) used SequenceImagePacker to pack images, args:
// Data\MaterialEffects\Textures\torch3 -sp torch -sx png -ss 62 -se 127 -sf 03 -ox 270 -oy 90 -fw 130 -fh 270 -outx jpg
// File saved as: Data/MaterialEffects/Textures/torch3/torchSEQ.jpg
// row 6, col 11, num images 66
// 4) then created an edgeMask image, size 130x270 (fw x fh), to mask out the edges when rendering

