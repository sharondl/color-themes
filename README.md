# Modeling How People Extract Color Themes from Images

[Project page](http://vis.stanford.edu/papers/color-themes)

Color choice plays an important role in works of graphic art and design. However, it can be difficult to choose a compelling set of colors, or color theme, from scratch. Thus, we look to images, which provide a good source for inspiring color themes.

This repository contains C# code for extracting color themes from images by using a regression model trained on themes created by people. This project shows how to extract color themes from a directory of images, output features and train a model, and compare themes with each other.

Main solution file is PaletteExtraction/PaletteExtraction.sln

## Needed inputs
* [C3](https://github.com/StanfordHCI/c3/) - Download and place in the same home directory as PaletteExtractor and Engine. Actually, only the [c3 data json file](https://github.com/StanfordHCI/c3/blob/master/data/xkcd/c3_data.json) is needed, but if you put the c3_data.json file in a different place, make sure to change the (json) path in localconfig.txt to point to it.
* Saliency maps for each image in the directory in PaletteExtraction/(dir)/saliency
* Segmentation maps for each image in the directory in PaletteExtraction/(dir)/segments
* localconfig.txt - Change the localconfig to change the directories from which files are read/outputted, though it should work as is.


For the saliency maps and segmentation maps, we used:

* [Judd Saliency code](http://people.csail.mit.edu/tjudd/WherePeopleLook/index.html) - It has a few dependencies, documented in its README. Some of the C code in the linked dependencies needs a bit of tweaking to work on Windows, but it shouldn't be too bad. For the Face Detection code, you may need to replace the opencv dlls with ones from the OpenCV distribution directly
* [Segmentation maps](http://www.cs.brown.edu/~pff/segment/) - This code only reads and outputs PBM files, so you may need to modify it to read/output other image formats

The code has been tested on Windows 7.

## Structure
* __PaletteExtraction__ - holds a simple GUI for extracting color themes, code for training a model, and comparing themes with each other
 * __images/__ - Holds images and their respective saliency and segmentation maps, from which to extract color themes. In the GUI, **extractThemes** extracts color themes into a text file, and **renderThemes** saves them as an image
 * __train/__ - Features are outputted here when the GUI option **calculateFeatures** is selected. There is a MATLAB script for training a model to extract color themes. Copy the fitted weights.txt and featureNames.txt to **weights/** in order to use them. 
 * __eval/__ - Holds the color theme datasets to train and compare on. Download the dataset [here](http://graphics.stanford.edu/~sharonl/papers/colorThemes-dataset.zip) and copy the set1 directory to **eval/**. The GUI option, **compareThemes** will calculate the distance and overlap between themes and save them as csv files. **extractThemesToCompare** will extract themes from the model, k-means, c-means, and randomly if the files do not already exist. **diagramThemes** will create an image diagram comparing the colors in each theme.
 * __weights/__ - Weights for the model, to use when extracting themes
 
* __Engine__ - the main library
 * __PaletteExtractor__ - extracts color themes from images with `HillClimbPalette()`
 
## Dataset
**Dataset 1** from the paper is available [here](http://graphics.stanford.edu/~sharonl/papers/colorThemes-dataset.zip). Download and copy the **eval/** directory to **PaletteExtraction**
 
## Additional Notes
**calculateFeatures** will generate 1000 random color themes with scores uniformly spread across a number of bins (if it hasn't already been generated). The code currently just uses rejection sampling, so this process can be very slow as it's hard to find very good or very bad color themes. Once the random themes are generated, the rest of the method takes about 4 hours for 40 (500x~300) images.

**Debug (Resize images)** option - This will resize images to be smaller (max 125x125), which will make color theme extraction faster. In the paper, we did not resize the images.

