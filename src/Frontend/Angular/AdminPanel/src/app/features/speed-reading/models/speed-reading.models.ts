export interface SpeedReadingText {
  id: string;
  title: string;
  content: string;
  wordCount: number;
  difficulty: 'beginner' | 'intermediate' | 'advanced' | 'expert';
  category: string;
  language: string;
  estimatedReadingTime: number;
  tags: string[];
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  createdBy: string;
}

export interface SpeedReadingExercise {
  id: string;
  textId: string;
  text?: SpeedReadingText;
  name: string;
  description: string;
  type: 'speed' | 'comprehension' | 'vocabulary' | 'mixed';
  wordsPerMinute: number;
  fontSize: number;
  displayMode: 'word' | 'phrase' | 'line' | 'paragraph';
  highlightColor?: string;
  backgroundColor?: string;
  duration?: number;
  questions?: ExerciseQuestion[];
  settings: ExerciseSettings;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface ExerciseQuestion {
  id: string;
  question: string;
  type: 'multiple-choice' | 'true-false' | 'open-ended';
  options?: string[];
  correctAnswer: string | boolean | number;
  points: number;
  explanation?: string;
}

export interface ExerciseSettings {
  autoAdvance: boolean;
  showProgress: boolean;
  enablePause: boolean;
  enableRewind: boolean;
  enableSpeedControl: boolean;
  minSpeed: number;
  maxSpeed: number;
  stepSize: number;
  fontFamily?: string;
  lineHeight?: number;
  letterSpacing?: number;
}

export interface UserProgress {
  id: string;
  userId: string;
  exerciseId: string;
  exercise?: SpeedReadingExercise;
  startedAt: Date;
  completedAt?: Date;
  wordsPerMinute: number;
  accuracy?: number;
  comprehensionScore?: number;
  totalWords: number;
  totalTime: number;
  status: 'in-progress' | 'completed' | 'abandoned';
  answers?: UserAnswer[];
}

export interface UserAnswer {
  questionId: string;
  answer: string | boolean | number;
  isCorrect: boolean;
  timeSpent: number;
}

export interface ProgressAnalytics {
  userId: string;
  totalExercises: number;
  completedExercises: number;
  averageWPM: number;
  averageAccuracy: number;
  averageComprehension: number;
  totalReadingTime: number;
  totalWordsRead: number;
  progressTrend: ProgressTrend[];
  strengthAreas: string[];
  improvementAreas: string[];
}

export interface ProgressTrend {
  date: Date;
  wpm: number;
  accuracy: number;
  comprehension: number;
  exercisesCompleted: number;
}

export interface CreateSpeedReadingTextDto {
  title: string;
  content: string;
  difficulty: 'beginner' | 'intermediate' | 'advanced' | 'expert';
  category: string;
  language: string;
  tags: string[];
}

export interface CreateExerciseDto {
  textId: string;
  name: string;
  description: string;
  type: 'speed' | 'comprehension' | 'vocabulary' | 'mixed';
  wordsPerMinute: number;
  fontSize: number;
  displayMode: 'word' | 'phrase' | 'line' | 'paragraph';
  duration?: number;
  questions?: ExerciseQuestion[];
  settings: ExerciseSettings;
}

export interface SpeedReadingFilter {
  difficulty?: 'beginner' | 'intermediate' | 'advanced' | 'expert';
  category?: string;
  type?: 'speed' | 'comprehension' | 'vocabulary' | 'mixed';
  minWPM?: number;
  maxWPM?: number;
  tags?: string[];
  isActive?: boolean;
}

export interface SpeedReadingStatistics {
  totalTexts: number;
  totalExercises: number;
  activeUsers: number;
  averageWPM: number;
  averageComprehension: number;
  popularCategories: CategoryStat[];
  difficultyDistribution: DifficultyDistribution;
  userEngagement: EngagementMetric[];
}

export interface CategoryStat {
  category: string;
  count: number;
  percentage: number;
}

export interface DifficultyDistribution {
  beginner: number;
  intermediate: number;
  advanced: number;
  expert: number;
}

export interface EngagementMetric {
  date: Date;
  activeUsers: number;
  exercisesCompleted: number;
  averageSessionTime: number;
}