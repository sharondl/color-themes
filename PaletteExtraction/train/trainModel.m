% Set the code directory and dataset
addpath('../../vendor/glmnet_matlab/')
imageNames = 'imageNames.txt';
dataset= 'features.csv';
featureNames= 'featurenames.txt';

% Test images that art students viewed
testImageNames = {'chikanobu2.png','homer1.png','macke1.png','seurat1.png','monet1.png','photo_krikit.png','photo_powi.png','photo_joaquin_rosado.png','photo_ssdginteriors.png','photo_radiofreebarton.png'};

% Analyze standardized weights? (not for fitting a model for use)
analyze = false;
trainThresh = 30; % Threshold of training images to use

palettesPerImage = 1000; % Assuming we generated 1000 random 

% Read in the features and set up training and test set
datapoints = {};
scoretarget = 1;

data = csvread(dataset);
datapoints.ids = data(:,1);
datapoints.targets = data(:,scoretarget+1);
datapoints.features = data(:,scoretarget+2:end);

% Just train on the randomly sampled color themes, and their people-based
% scores (ids are < 0), not the ones created by people directly
ridx = find(datapoints.ids < 0);
datapoints.ids = datapoints.ids(ridx);
datapoints.targets = datapoints.targets(ridx);
datapoints.features = datapoints.features(ridx,:);

% Standardize the features if needed
if (analyze)
 for i=1:size(datapoints.features,2)
     temp = 0;
     if (std(datapoints.features(:,i))>0)
        temp = (datapoints.features(:,i)-mean(datapoints.features(:,i)))/std(datapoints.features(:,i));
     end
    datapoints.features(:,i) = temp;
 end
end 

% Organize datapoints by image
numPts=size(datapoints.features,1);
numImages = numPts/palettesPerImage;
imageToIndices = zeros(numImages, palettesPerImage);
for i=1:numImages
   imageToIndices(i, 1:palettesPerImage) = ((i-1)*palettesPerImage+1):(i*palettesPerImage);
end

fid = fopen(featureNames);
fnames = textscan(fid, '%s', 'delimiter', '\n');
fclose(fid);

fid = fopen(imageNames);
inames = textscan(fid, '%s', 'delimiter','\n');
fclose(fid);

datapoints.featureNames = fnames{1};
datapoints.imageNames = inames{1};

namePerm = randperm(length(datapoints.features(1,:)));
datapoints.features = datapoints.features(:, namePerm);
datapoints.featureNames = datapoints.featureNames(namePerm);

testImages = zeros(1,length(testImageNames));
for i=1:length(testImageNames)
    testImages(i) = find(strcmp(testImageNames(i),datapoints.imageNames));
end

trainingImages = setdiff(1:numImages, testImages);
trainingImages = trainingImages(randperm(length(trainingImages))); 

% Possibly limit number of training images
trainingImages = trainingImages(1:trainThresh);

trainingPts=reshape(imageToIndices(trainingImages,:)',length(trainingImages)*palettesPerImage,1);
testingPts=reshape(imageToIndices(testImages,:)', length(testImages)*palettesPerImage,1);

leaveout = max(1,trainThresh/10);

% Compute folds for finding lambda to minimize cross-validation error
foldids = zeros(1,length(trainingPts));
for i=1:length(trainingImages)
    actual = floor((i-1)/leaveout)+1;
    foldids(((i-1)*palettesPerImage+1):(i*palettesPerImage)) = actual;
end

% Find/Set lambda and train the LASSO regressor
options=glmnetSet(); 
options.standardize = ~analyze; 

lamb = cvglmnet(datapoints.features(trainingPts,:), datapoints.targets(trainingPts),10,foldids,'response','gaussian',options,1);

options.lambda = lamb.lambda_min; %7.3066e-04 (value used for CHI)
options.lambda

fit = glmnet(datapoints.features(trainingPts,:), datapoints.targets(trainingPts),'gaussian',options);

%predict test set
testingTargets=datapoints.targets(testingPts);
testingPredictions = glmnetPredict(fit, 'response', datapoints.features(testingPts,:));
trainingPredictions = glmnetPredict(fit, 'response', datapoints.features(trainingPts,:));

% Compute the mean absolute error for regressor and for the fixed estimator baseline
meanAbsErr = mean(abs(testingTargets-testingPredictions))
fixedMeanAbsErr = mean(abs(testingTargets-mean(datapoints.targets(testingPts))))

% Compute the mean squared error for regressor and for the fixed estimator baseline
meanSqdErr = mean((testingTargets-testingPredictions).^2)
fixedMeanSqrErr = mean((testingTargets-mean(datapoints.targets(testingPts))).^2)


% Output sorted weights, and save the fit
fprintf('Sorted weights\n');
[sorted, idx] = sort(-1*abs(fit.beta));

for i=1:length(datapoints.featureNames)
    fprintf('%d %s, %.4f \n ',i, datapoints.featureNames{idx(i)},fit.beta(idx(i)))
end

% Save the weights
if (~analyze)
    dlmwrite('fittedWeights.csv',fit.beta);
end